using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace Kopdar.Services;

public interface IStorageService
{
    Task<string> UploadFileAsync(string fileName, Stream fileStream);
    Task DeleteFileAsync(string fileUrl);
}

// ---------------------------------------------------------
// 1. Local File System Storage (Default)
// ---------------------------------------------------------
public class LocalStorageService : IStorageService
{
    private readonly string _storageFolder = "wwwroot/uploads";

    public LocalStorageService()
    {
        if (!Directory.Exists(_storageFolder))
        {
            Directory.CreateDirectory(_storageFolder);
        }
    }

    public async Task<string> UploadFileAsync(string fileName, Stream fileStream)
    {
        var safeFileName = Guid.NewGuid().ToString() + Path.GetExtension(fileName);
        var path = Path.Combine(_storageFolder, safeFileName);
        
        using var stream = new FileStream(path, FileMode.Create);
        await fileStream.CopyToAsync(stream);
        
        return $"/uploads/{safeFileName}";
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        var fileName = Path.GetFileName(fileUrl);
        var path = Path.Combine(_storageFolder, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }
}

// ---------------------------------------------------------
// 2. Azure Blob Storage Implementation (Mocked for Demo / Template ready)
// ---------------------------------------------------------
public class AzureBlobStorageService : IStorageService
{
    private readonly string _connectionString;
    private readonly string _containerName;
    BlobServiceClient _blobServiceClient { set; get; }
    public AzureBlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureBlob:ConnectionString"] ?? "UseDevelopmentStorage=true";
        _containerName = configuration["AzureBlob:ContainerName"] ?? "kopdar-uploads";
        
        // Setup Azure BlobServiceClient here using Azure.Storage.Blobs SDK
        _blobServiceClient = new BlobServiceClient(_connectionString);
        Console.WriteLine($"[AzureBlobStorage] Initialized with Container: {_containerName}");
    }

    public async Task<string> UploadFileAsync(string fileName, Stream fileStream)
    {
        var safeFileName = Guid.NewGuid().ToString() + Path.GetExtension(fileName);
        
        // Mock Implementation:
         var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
         await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
         var blobClient = containerClient.GetBlobClient(safeFileName);
         await blobClient.UploadAsync(fileStream, true);
         return blobClient.Uri.ToString();
        
        //await Task.Delay(500); // Simulate upload delay
        //var mockUrl = $"https://kopdarazure.blob.core.windows.net/{_containerName}/{safeFileName}";
        //Console.WriteLine($"[AzureBlobStorage] Uploaded to: {mockUrl}");
        
        //return mockUrl;
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        // Mock Implementation: Extract blob name and delete
        Console.WriteLine($"[AzureBlobStorage] Deleted file: {fileUrl}");
        return Task.CompletedTask;
    }
}

// ---------------------------------------------------------
// 3. AWS S3 Storage Implementation (Mocked for Demo / Template ready)
// ---------------------------------------------------------
public class AwsS3StorageService : IStorageService
{
    private readonly string _bucketName;
    private readonly string _region;

    public AwsS3StorageService(IConfiguration configuration)
    {
        _bucketName = configuration["AwsS3:BucketName"] ?? "kopdar-uploads-bucket";
        _region = configuration["AwsS3:Region"] ?? "us-east-1";
        
        // Setup AmazonS3Client here using AWSSDK.S3 library
        // e.g. _s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(_region));
        Console.WriteLine($"[AwsS3Storage] Initialized with Bucket: {_bucketName} in Region: {_region}");
    }

    public async Task<string> UploadFileAsync(string fileName, Stream fileStream)
    {
        var safeFileName = Guid.NewGuid().ToString() + Path.GetExtension(fileName);
        
        // Mock Implementation:
        // var putRequest = new PutObjectRequest
        // {
        //     BucketName = _bucketName,
        //     Key = safeFileName,
        //     InputStream = fileStream,
        //     CannedACL = S3CannedACL.PublicRead
        // };
        // await _s3Client.PutObjectAsync(putRequest);
        // return $"https://{_bucketName}.s3.{_region}.amazonaws.com/{safeFileName}";

        await Task.Delay(500); // Simulate upload delay
        var mockUrl = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{safeFileName}";
        Console.WriteLine($"[AwsS3Storage] Uploaded to: {mockUrl}");
        
        return mockUrl;
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        // Mock Implementation: Extract S3 Key and delete object
        Console.WriteLine($"[AwsS3Storage] Deleted file: {fileUrl}");
        return Task.CompletedTask;
    }
}
