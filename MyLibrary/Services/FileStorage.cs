using Microsoft.Extensions.Options;

namespace MyLibrary.Services;

public class FileStorageOptions
{
    public string Provider { get; set; } = "FileSystem"; // FileSystem, AzureBlob, AwsS3
    public string BasePath { get; set; } = "storage";
}

public interface IFileStorage
{
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
}

public class FileSystemFileStorage : IFileStorage
{
    private readonly FileStorageOptions _options;
    private readonly IWebHostEnvironment _env;

    public FileSystemFileStorage(IOptions<FileStorageOptions> options, IWebHostEnvironment env)
    {
        _options = options.Value;
        _env = env;
    }

    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(_env.ContentRootPath, _options.BasePath);
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);

        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return filePath;
    }
}
