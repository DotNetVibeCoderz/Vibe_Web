using Lapak.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Lapak.Services.Storage;

/// <summary>
/// Abstract file storage service supporting multiple backends
/// </summary>
public interface IStorageService
{
    Task<string> UploadAsync(string fileName, Stream fileStream, string contentType);
    Task<Stream?> DownloadAsync(string filePath);
    Task<bool> DeleteAsync(string filePath);
    Task<string> GetPublicUrlAsync(string filePath);
}

/// <summary>
/// Local file system storage implementation
/// </summary>
public class FileSystemStorageService : IStorageService
{
    private readonly string _rootPath;
    private readonly string _baseUrl;
    private readonly IWebHostEnvironment _env;

    public FileSystemStorageService(IOptions<StorageConfig> config, IWebHostEnvironment env)
    {
        _rootPath = config.Value.FileSystem.RootPath;
        _baseUrl = config.Value.FileSystem.BaseUrl;
        _env = env;
    }

    public async Task<string> UploadAsync(string fileName, Stream fileStream, string contentType)
    {
        var uploadDir = Path.Combine(_env.ContentRootPath, _rootPath);
        Directory.CreateDirectory(uploadDir);

        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(uploadDir, uniqueName);

        using var fs = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fs);

        return uniqueName;
    }

    public Task<Stream?> DownloadAsync(string filePath)
    {
        var fullPath = Path.Combine(_env.ContentRootPath, _rootPath, filePath);
        if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
    }

    public Task<bool> DeleteAsync(string filePath)
    {
        var fullPath = Path.Combine(_env.ContentRootPath, _rootPath, filePath);
        if (!File.Exists(fullPath)) return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public Task<string> GetPublicUrlAsync(string filePath)
    {
        return Task.FromResult($"{_baseUrl}/{filePath}");
    }
}

/// <summary>
/// MinIO / S3-compatible storage implementation
/// Note: Full MinIO implementation requires proper client setup.
/// This is a simplified version.
/// </summary>
public class MinioStorageService : IStorageService
{
    private readonly MinioStorageConfig _config;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IOptions<StorageConfig> config, ILogger<MinioStorageService> logger)
    {
        _config = config.Value.MinIO;
        _logger = logger;
    }

    public async Task<string> UploadAsync(string fileName, Stream fileStream, string contentType)
    {
        // MinIO upload implementation would go here
        // For now, generate a unique name and log
        var objectName = $"{Guid.NewGuid():N}_{fileName}";
        _logger.LogInformation("MinIO upload placeholder: {ObjectName} ({Size} bytes)", objectName, fileStream.Length);
        await Task.CompletedTask;
        return objectName;
    }

    public Task<Stream?> DownloadAsync(string filePath)
    {
        _logger.LogInformation("MinIO download placeholder: {FilePath}", filePath);
        return Task.FromResult<Stream?>(null);
    }

    public Task<bool> DeleteAsync(string filePath)
    {
        _logger.LogInformation("MinIO delete placeholder: {FilePath}", filePath);
        return Task.FromResult(true);
    }

    public Task<string> GetPublicUrlAsync(string filePath)
    {
        var protocol = _config.UseSsl ? "https" : "http";
        return Task.FromResult($"{protocol}://{_config.Endpoint}/{_config.BucketName}/{filePath}");
    }
}

/// <summary>
/// Storage service factory - selects the appropriate implementation based on configuration
/// </summary>
public class StorageServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StorageConfig _config;

    public StorageServiceFactory(IServiceProvider serviceProvider, IOptions<StorageConfig> config)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
    }

    public IStorageService GetStorageService()
    {
        return _config.Provider switch
        {
            "MinIO" => _serviceProvider.GetRequiredService<MinioStorageService>(),
            "AmazonS3" => _serviceProvider.GetRequiredService<MinioStorageService>(),
            "AzureBlob" => _serviceProvider.GetRequiredService<FileSystemStorageService>(),
            _ => _serviceProvider.GetRequiredService<FileSystemStorageService>()
        };
    }
}
