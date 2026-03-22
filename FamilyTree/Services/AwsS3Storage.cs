namespace FamilyTree.Services;

public class AwsS3Storage : IFileStorage
{
    public Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        // Placeholder for AWS S3 integration
        throw new NotImplementedException("AWS S3 integration belum dikonfigurasi.");
    }
}
