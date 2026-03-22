using Microsoft.Extensions.Options;

namespace FamilyTree.Services;

public class FileSystemStorage : IFileStorage
{
    private readonly StorageSettings _settings;
    private readonly IWebHostEnvironment _environment;

    public FileSystemStorage(IOptions<StorageSettings> settings, IWebHostEnvironment environment)
    {
        _settings = settings.Value;
        _environment = environment;
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var uploadsRoot = Path.Combine(_environment.ContentRootPath, _settings.UploadRoot);
        if (!Directory.Exists(uploadsRoot))
        {
            Directory.CreateDirectory(uploadsRoot);
        }

        var safeFileName = $"{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(uploadsRoot, safeFileName);
        await using var output = File.Create(filePath);
        await stream.CopyToAsync(output, cancellationToken);

        return $"/uploads/{safeFileName}";
    }
}
