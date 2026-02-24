using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace MyPoS.Services
{
    public class AwsS3StorageService : IStorageService
    {
        private readonly StorageConfig _config;

        public AwsS3StorageService(IOptions<StorageConfig> config)
        {
            _config = config.Value;
        }

        public async Task<string> UploadFileAsync(IBrowserFile file, string fileName)
        {
            if (string.IsNullOrEmpty(_config.BucketOrContainerName))
                throw new InvalidOperationException("AWS S3 bucket name is not configured.");

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var s3Client = new AmazonS3Client(); // expects env variables or IAM roles to be configured

            using var memoryStream = new MemoryStream();
            await file.OpenReadStream(10485760).CopyToAsync(memoryStream); // 10MB
            
            var transferUtility = new TransferUtility(s3Client);
            await transferUtility.UploadAsync(memoryStream, _config.BucketOrContainerName, uniqueFileName);

            var fileUrl = $"https://{_config.BucketOrContainerName}.s3.amazonaws.com/{uniqueFileName}";
            return fileUrl;
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl) || string.IsNullOrEmpty(_config.BucketOrContainerName)) return;

            var uri = new Uri(fileUrl);
            var key = Path.GetFileName(uri.LocalPath);

            var s3Client = new AmazonS3Client();
            await s3Client.DeleteObjectAsync(_config.BucketOrContainerName, key);
        }
    }
}
