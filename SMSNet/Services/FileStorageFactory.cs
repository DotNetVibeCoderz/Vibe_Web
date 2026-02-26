using Microsoft.Extensions.Options;

namespace SMSNet.Services;

public interface IFileStorageFactory
{
    IFileStorage Create();
}

public class FileStorageFactory : IFileStorageFactory
{
    private readonly FileStorageOptions _options;

    public FileStorageFactory(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
    }

    public IFileStorage Create()
    {
        return _options.Provider.ToLowerInvariant() switch
        {
            "azureblob" => new AzureBlobStorage(),
            "awss3" => new AwsS3Storage(),
            _ => new FileSystemStorage(_options)
        };
    }
}
