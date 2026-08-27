using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace MyPoS.Services
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly StorageConfig _config;

        public AzureBlobStorageService(IOptions<StorageConfig> config)
        {
            _config = config.Value;
        }

        public async Task<string> UploadFileAsync(IBrowserFile file, string fileName)
        {
            if (string.IsNullOrEmpty(_config.ConnectionString))
                throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var containerClient = new BlobContainerClient(_config.ConnectionString, _config.BucketOrContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(uniqueFileName);
            
            using var stream = file.OpenReadStream(10485760); // Max 10MB
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return blobClient.Uri.ToString();
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl) || string.IsNullOrEmpty(_config.ConnectionString)) return;

            var uri = new Uri(fileUrl);
            var blobName = Path.GetFileName(uri.LocalPath);
            
            var containerClient = new BlobContainerClient(_config.ConnectionString, _config.BucketOrContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
