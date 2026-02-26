namespace SMSNet.Services;

// Placeholder services for future Azure Blob or AWS S3 integration.
public class AzureBlobStorage : IFileStorage
{
    public Task<string> SaveAsync(string fileName, Stream fileStream, CancellationToken cancellationToken = default)
    {
        // TODO: Implement Azure Blob storage.
        return Task.FromResult($"/uploads/azure/{fileName}");
    }
}

public class AwsS3Storage : IFileStorage
{
    public Task<string> SaveAsync(string fileName, Stream fileStream, CancellationToken cancellationToken = default)
    {
        // TODO: Implement AWS S3 storage.
        return Task.FromResult($"/uploads/s3/{fileName}");
    }
}
