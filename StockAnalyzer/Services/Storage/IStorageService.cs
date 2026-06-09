namespace StockAnalyzer.Services.Storage;

/// <summary>
/// Storage service interface for file operations.
/// Supports multiple backends: FileSystem, MinIO, S3, Azure Blob.
/// </summary>
public interface IStorageService
{
    Task<string> UploadAsync(string fileName, Stream content);
    Task<Stream?> DownloadAsync(string fileName);
    Task<bool> DeleteAsync(string fileName);
    Task<bool> ExistsAsync(string fileName);
    Task<List<string>> ListFilesAsync(string? prefix = null);
    Task<string> GetPublicUrlAsync(string fileName);
}

/// <summary>
/// Factory for creating storage service instances.
/// </summary>
public interface IStorageServiceFactory
{
    IStorageService CreateStorageService();
}
