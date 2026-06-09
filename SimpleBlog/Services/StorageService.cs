using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Amazon.S3;
using Amazon.S3.Model;

namespace SimpleBlog.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task DeleteFileAsync(string fileUrl);
    }

    // --- File System Implementation ---
    public class FileSystemStorageService : IStorageService
    {
        private readonly string _uploadPath;

        public FileSystemStorageService(IConfiguration config)
        {
            _uploadPath = config["StorageSettings:FileSystemPath"] ?? "wwwroot/uploads";
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(_uploadPath, uniqueFileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(stream);
            }

            return $"/uploads/{uniqueFileName}";
        }

        public Task DeleteFileAsync(string fileUrl)
        {
            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_uploadPath, fileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            return Task.CompletedTask;
        }
    }

    // --- Azure Blob Storage Implementation ---
    public class AzureBlobStorageService : IStorageService
    {
        private readonly BlobContainerClient? _containerClient;

        public AzureBlobStorageService(IConfiguration config)
        {
            var connectionString = config["StorageSettings:AzureConnectionString"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                var containerName = config["StorageSettings:AzureContainer"] ?? "blogs";
                var serviceClient = new BlobServiceClient(connectionString);
                _containerClient = serviceClient.GetBlobContainerClient(containerName);
                _containerClient.CreateIfNotExists();
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            if (_containerClient == null) throw new InvalidOperationException("Azure Storage not configured.");

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = _containerClient.GetBlobClient(uniqueFileName);
            
            await blobClient.UploadAsync(fileStream, new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType });

            return blobClient.Uri.ToString();
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (_containerClient == null) return;

            var uri = new Uri(fileUrl);
            var blobName = Path.GetFileName(uri.LocalPath);
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }

    // --- AWS S3 Storage Implementation ---
    public class S3StorageService : IStorageService
    {
        private readonly IAmazonS3? _s3Client;
        private readonly string _bucketName;

        public S3StorageService(IConfiguration config)
        {
            var accessKey = config["StorageSettings:S3AccessKey"];
            var secretKey = config["StorageSettings:S3SecretKey"];
            _bucketName = config["StorageSettings:S3Bucket"] ?? "";
            
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                _s3Client = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.USEast1);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            if (_s3Client == null) throw new InvalidOperationException("S3 Storage not configured.");

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = uniqueFileName,
                InputStream = fileStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead
            };

            await _s3Client.PutObjectAsync(request);

            return $"https://{_bucketName}.s3.amazonaws.com/{uniqueFileName}";
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (_s3Client == null) return;

            var uri = new Uri(fileUrl);
            var key = Path.GetFileName(uri.LocalPath);

            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
        }
    }
}
