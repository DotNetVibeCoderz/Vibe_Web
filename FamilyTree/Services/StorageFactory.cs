using FamilyTree.Models;
using Microsoft.Extensions.Options;

namespace FamilyTree.Services;

public class StorageFactory
{
    private readonly IServiceProvider _services;
    private readonly StorageSettings _settings;

    public StorageFactory(IServiceProvider services, IOptions<StorageSettings> settings)
    {
        _services = services;
        _settings = settings.Value;
    }

    public IFileStorage Create()
    {
        var providerName = _settings.Provider?.Trim() ?? "FileSystem";

        if (providerName.Equals("AzureBlobStorage", StringComparison.OrdinalIgnoreCase))
        {
            providerName = "AzureBlob";
        }

        if (!Enum.TryParse<StorageProvider>(providerName, true, out var provider))
        {
            provider = StorageProvider.FileSystem;
        }

        return provider switch
        {
            StorageProvider.AzureBlob => _services.GetRequiredService<AzureBlobStorage>(),
            StorageProvider.AwsS3 => _services.GetRequiredService<AwsS3Storage>(),
            _ => _services.GetRequiredService<FileSystemStorage>()
        };
    }
}
