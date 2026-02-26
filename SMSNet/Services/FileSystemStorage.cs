namespace SMSNet.Services;

public class FileSystemStorage : IFileStorage
{
    private readonly string _basePath;

    public FileSystemStorage(FileStorageOptions options)
    {
        _basePath = options.BasePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(string fileName, Stream fileStream, CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(_basePath, safeFileName);

        await using var output = File.Create(fullPath);
        await fileStream.CopyToAsync(output, cancellationToken);

        return fullPath.Replace("wwwroot", string.Empty).Replace("\\", "/");
    }
}
