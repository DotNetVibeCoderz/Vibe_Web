using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;

namespace SMSNet.Services;

/// <summary>What an operator can change about uploads without a rebuild.
/// Bound from the <c>Uploads</c> section of appsettings.json.</summary>
public class UploadSettings
{
    public const string SectionName = "Uploads";

    public long MaxFileSizeBytes { get; set; } = 15 * 1024 * 1024;

    public int MaxFilesPerItem { get; set; } = 5;

    /// <summary>Extensions accepted, lower case, leading dot.</summary>
    public string[] AllowedExtensions { get; set; } =
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".md", ".rtf", ".odt",
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp",
        ".zip"
    };

    public string[] ImageExtensions { get; set; } = { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };

    /// <summary>Folder under wwwroot where uploads land.</summary>
    public string SubFolder { get; set; } = "uploads/files";
}

public sealed record StoredFile(
    string FileName,
    string ContentType,
    string Url,
    long SizeBytes,
    bool IsImage);

public sealed record FileUploadResult(StoredFile? File, string? Error);

/// <summary>
/// Saves a user-supplied file under wwwroot and returns the URL to link to.
/// <para>
/// The client's filename never reaches the filesystem: it can contain path
/// traversal ("../../appsettings.json"), and two users uploading "rapor.pdf"
/// would otherwise overwrite each other. The stored name is generated and only
/// a vetted extension is carried over.
/// </para>
/// </summary>
public sealed class UploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<UploadSettings> _settings;
    private readonly ILogger<UploadService> _logger;

    public UploadService(
        IWebHostEnvironment environment,
        IOptionsMonitor<UploadSettings> settings,
        ILogger<UploadService> logger)
    {
        _environment = environment;
        _settings = settings;
        _logger = logger;
    }

    public UploadSettings Settings => _settings.CurrentValue;

    /// <summary>The <c>accept</c> attribute for a file input, from the same allowlist.</summary>
    public string AcceptAttribute => string.Join(",", _settings.CurrentValue.AllowedExtensions);

    public async Task<FileUploadResult> SaveAsync(
        IBrowserFile file,
        string? subFolder = null,
        CancellationToken cancellationToken = default)
    {
        var settings = _settings.CurrentValue;

        if (file.Size == 0)
        {
            return new FileUploadResult(null, $"{file.Name} kosong.");
        }

        if (file.Size > settings.MaxFileSizeBytes)
        {
            return new FileUploadResult(null,
                $"{file.Name} berukuran {Human(file.Size)} — melebihi batas {Human(settings.MaxFileSizeBytes)}.");
        }

        var extension = Path.GetExtension(file.Name).ToLowerInvariant();

        if (!settings.AllowedExtensions.Contains(extension))
        {
            return new FileUploadResult(null,
                $"Tipe berkas {extension} tidak didukung. Yang diizinkan: {string.Join(", ", settings.AllowedExtensions)}.");
        }

        try
        {
            var folderName = string.IsNullOrWhiteSpace(subFolder) ? settings.SubFolder : subFolder;
            var folder = Path.Combine(
                _environment.WebRootPath,
                folderName.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(folder);

            var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}";
            var absolutePath = Path.Combine(folder, storedName);

            await using (var source = file.OpenReadStream(settings.MaxFileSizeBytes, cancellationToken))
            await using (var destination = File.Create(absolutePath))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            return new FileUploadResult(new StoredFile(
                FileName: Path.GetFileName(file.Name),
                ContentType: file.ContentType ?? "application/octet-stream",
                Url: $"/{folderName.Trim('/')}/{storedName}",
                SizeBytes: file.Size,
                IsImage: settings.ImageExtensions.Contains(extension)), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Upload failed for {FileName}", file.Name);
            return new FileUploadResult(null, $"Gagal menyimpan {file.Name}: {ex.Message}");
        }
    }

    public static string Human(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / 1024d / 1024d:0.#} MB",
        >= 1024 => $"{bytes / 1024d:0.#} KB",
        _ => $"{bytes} B"
    };
}
