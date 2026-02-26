namespace SMSNet.Services;

public interface IFileStorage
{
    Task<string> SaveAsync(string fileName, Stream fileStream, CancellationToken cancellationToken = default);
}
