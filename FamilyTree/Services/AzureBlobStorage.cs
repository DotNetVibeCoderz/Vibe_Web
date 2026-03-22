using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace FamilyTree.Services;

public class AzureBlobStorage : IFileStorage
{
    private readonly AzureBlobSettings _settings;

    public AzureBlobStorage(IOptions<StorageSettings> settings)
    {
        _settings = settings.Value.AzureBlob;
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage belum dikonfigurasi. Isi Storage:AzureBlob:ConnectionString di appsettings.");
        }

        if (string.IsNullOrWhiteSpace(_settings.ContainerName))
        {
            throw new InvalidOperationException("Azure Blob Storage belum dikonfigurasi. Isi Storage:AzureBlob:ContainerName di appsettings.");
        }

        var containerClient = new BlobContainerClient(_settings.ConnectionString, _settings.ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(fileName);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        stream.Position = 0;
        await blobClient.UploadAsync(stream, options, cancellationToken);
        return blobClient.Uri.ToString();
    }
}
