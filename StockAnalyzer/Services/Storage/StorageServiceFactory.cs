namespace StockAnalyzer.Services.Storage;

/// <summary>
/// Factory for creating storage service instances based on configuration.
/// </summary>
public class StorageServiceFactory : IStorageServiceFactory
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StorageServiceFactory> _logger;

    public StorageServiceFactory(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<StorageServiceFactory> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Create the appropriate storage service based on configuration.
    /// </summary>
    public IStorageService CreateStorageService()
    {
        var provider = _configuration["Storage:Provider"] ?? "FileSystem";

        return provider switch
        {
            "MinIO" or "S3" => (IStorageService)_serviceProvider.GetRequiredService<S3StorageService>(),
            "AzureBlob" => (IStorageService)_serviceProvider.GetRequiredService<AzureBlobStorageService>(),
            "FileSystem" or _ => (IStorageService)_serviceProvider.GetRequiredService<FileSystemStorageService>()
        };
    }
}
