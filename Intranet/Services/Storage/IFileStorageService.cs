namespace Intranet.Services.Storage;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName);
    Task DeleteFileAsync(string filePath);
    string GetFileUrl(string filePath);
}
