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

        // BrowserFileStream tidak mendukung Position/Seek.
        // Jadi kita normalisasi stream ke MemoryStream agar aman di-upload.
        Stream uploadStream = stream;
        if (!stream.CanSeek)
        {
            var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            uploadStream = buffer;
        }
        else
        {
            stream.Position = 0;
        }

        await blobClient.UploadAsync(uploadStream, options, cancellationToken);

        if (uploadStream is MemoryStream)
        {
            await uploadStream.DisposeAsync();
        }

        return blobClient.Uri.ToString();
    }
}
