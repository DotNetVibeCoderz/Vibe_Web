using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using SMSNet.Models;

namespace SMSNet.Services.Assistant;

public sealed record UploadOutcome(PendingAttachment? Attachment, string? Error);

/// <summary>
/// Stores files a user attaches to a chat message and hands back a URL the
/// assistant can reference.
/// <para>
/// Uploads are model-facing input, so the content type is taken from the
/// allowlist rather than the browser's claim, and the stored filename is
/// generated — never the client's, which can contain path traversal.
/// </para>
/// </summary>
public sealed class ChatUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<AssistantOptions> _options;
    private readonly ILogger<ChatUploadService> _logger;

    public ChatUploadService(
        IWebHostEnvironment environment,
        IOptionsMonitor<AssistantOptions> options,
        ILogger<ChatUploadService> logger)
    {
        _environment = environment;
        _options = options;
        _logger = logger;
    }

    public AssistantOptions.UploadOptions Limits => _options.CurrentValue.Uploads;

    public async Task<UploadOutcome> SaveAsync(IBrowserFile file, CancellationToken ct = default)
    {
        var limits = _options.CurrentValue.Uploads;

        if (file.Size > limits.MaxFileSizeBytes)
        {
            return new UploadOutcome(null,
                $"{file.Name} berukuran {Human(file.Size)} — melebihi batas {Human(limits.MaxFileSizeBytes)}.");
        }

        if (file.Size == 0)
        {
            return new UploadOutcome(null, $"{file.Name} kosong.");
        }

        var contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();
        var isImage = limits.AllowedImageTypes.Contains(contentType);
        var isDocument = limits.AllowedDocumentTypes.Contains(contentType);

        if (!isImage && !isDocument)
        {
            return new UploadOutcome(null,
                $"Tipe berkas {file.Name} ({contentType}) tidak didukung.");
        }

        try
        {
            var folder = Path.Combine(_environment.WebRootPath, limits.SubFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folder);

            // The client's filename never touches the filesystem.
            var extension = SafeExtension(file.Name);
            var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}";
            var absolutePath = Path.Combine(folder, storedName);

            await using (var source = file.OpenReadStream(limits.MaxFileSizeBytes, ct))
            await using (var destination = File.Create(absolutePath))
            {
                await source.CopyToAsync(destination, ct);
            }

            var url = $"/{limits.SubFolder.Trim('/')}/{storedName}";

            return new UploadOutcome(new PendingAttachment(
                FileName: Path.GetFileName(file.Name),
                ContentType: contentType,
                Url: url,
                SizeBytes: file.Size,
                Kind: isImage ? AttachmentKind.Image : AttachmentKind.Document,
                AbsolutePath: absolutePath), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Chat upload failed for {FileName}", file.Name);
            return new UploadOutcome(null, $"Gagal menyimpan {file.Name}: {ex.Message}");
        }
    }

    private static string SafeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 12)
        {
            return string.Empty;
        }

        return extension.All(c => char.IsLetterOrDigit(c) || c == '.')
            ? extension.ToLowerInvariant()
            : string.Empty;
    }

    public static string Human(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / 1024d / 1024d:0.#} MB",
        >= 1024 => $"{bytes / 1024d:0.#} KB",
        _ => $"{bytes} B"
    };
}
