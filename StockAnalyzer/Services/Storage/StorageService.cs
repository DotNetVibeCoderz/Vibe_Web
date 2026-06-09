namespace StockAnalyzer.Services.Storage;

/// <summary>
/// FileSystem storage implementation.
/// Stores files locally on the server's file system.
/// </summary>
public class FileSystemStorageService : IStorageService
{
    private readonly string _basePath;
    private readonly ILogger<FileSystemStorageService> _logger;

    public FileSystemStorageService(IConfiguration configuration, ILogger<FileSystemStorageService> logger)
    {
        _basePath = configuration["Storage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Data", "Storage");
        _logger = logger;

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> UploadAsync(string fileName, Stream content)
    {
        var safeFileName = SanitizeFileName(fileName);
        var filePath = Path.Combine(_basePath, safeFileName);
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream);

        _logger.LogInformation("File uploaded: {FilePath}", filePath);
        return safeFileName;
    }

    public Task<Stream?> DownloadAsync(string fileName)
    {
        var filePath = Path.Combine(_basePath, SanitizeFileName(fileName));

        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        var stream = File.OpenRead(filePath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string fileName)
    {
        var filePath = Path.Combine(_basePath, SanitizeFileName(fileName));

        if (!File.Exists(filePath))
            return Task.FromResult(false);

        File.Delete(filePath);
        _logger.LogInformation("File deleted: {FilePath}", filePath);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string fileName)
    {
        var filePath = Path.Combine(_basePath, SanitizeFileName(fileName));
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<List<string>> ListFilesAsync(string? prefix = null)
    {
        var files = Directory.GetFiles(_basePath, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(_basePath, f))
            .ToList();

        if (!string.IsNullOrEmpty(prefix))
            files = files.Where(f => f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult(files);
    }

    public Task<string> GetPublicUrlAsync(string fileName)
    {
        // For file system, return a relative URL
        return Task.FromResult($"/storage/{fileName}");
    }

    private string SanitizeFileName(string fileName)
    {
        // Remove invalid characters
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Replace("..", "_");
    }
}

/// <summary>
/// S3-compatible storage (MinIO, AWS S3, etc.).
/// Placeholder with basic implementation structure.
/// </summary>
public class S3StorageService : IStorageService
{
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IConfiguration configuration, ILogger<S3StorageService> logger)
    {
        _logger = logger;
        // S3 client initialization would go here
        // For production, use AWSSDK.S3 or MinIO SDK
    }

    public Task<string> UploadAsync(string fileName, Stream content)
    {
        _logger.LogInformation("S3 Upload: {FileName}", fileName);
        // Implement S3/MinIO upload
        throw new NotImplementedException("S3 storage requires AWSSDK.S3 package. Use FileSystem storage for now.");
    }

    public Task<Stream?> DownloadAsync(string fileName) => throw new NotImplementedException();
    public Task<bool> DeleteAsync(string fileName) => throw new NotImplementedException();
    public Task<bool> ExistsAsync(string fileName) => throw new NotImplementedException();
    public Task<List<string>> ListFilesAsync(string? prefix = null) => throw new NotImplementedException();
    public Task<string> GetPublicUrlAsync(string fileName) => throw new NotImplementedException();
}

/// <summary>
/// Azure Blob storage implementation placeholder.
/// </summary>
public class AzureBlobStorageService : IStorageService
{
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadAsync(string fileName, Stream content)
    {
        _logger.LogInformation("Azure Blob Upload: {FileName}", fileName);
        throw new NotImplementedException("Azure Blob storage requires Azure.Storage.Blobs package. Use FileSystem storage for now.");
    }

    public Task<Stream?> DownloadAsync(string fileName) => throw new NotImplementedException();
    public Task<bool> DeleteAsync(string fileName) => throw new NotImplementedException();
    public Task<bool> ExistsAsync(string fileName) => throw new NotImplementedException();
    public Task<List<string>> ListFilesAsync(string? prefix = null) => throw new NotImplementedException();
    public Task<string> GetPublicUrlAsync(string fileName) => throw new NotImplementedException();
}
