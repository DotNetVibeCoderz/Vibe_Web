namespace EasyParking.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(byte[] content, string fileName);
    Task DeleteFileAsync(string filePath);
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

    public LocalFileStorageService()
    {
        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> SaveFileAsync(byte[] content, string fileName)
    {
        var filePath = Path.Combine(_storagePath, fileName);
        await File.WriteAllBytesAsync(filePath, content);
        return $"/uploads/{fileName}";
    }

    public Task DeleteFileAsync(string filePath)
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
