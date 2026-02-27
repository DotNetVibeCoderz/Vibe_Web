using System.IO;
using System.Threading.Tasks;

namespace AISTracker.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(string fileName, Stream content);
        Task<Stream> GetFileAsync(string filePath);
        Task DeleteFileAsync(string filePath);
    }

    public class FileSystemStorageService : IFileStorageService
    {
        private readonly string _storagePath;

        public FileSystemStorageService(IConfiguration configuration)
        {
            _storagePath = configuration.GetValue<string>("FileStoragePath") ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public async Task<string> SaveFileAsync(string fileName, Stream content)
        {
            var filePath = Path.Combine(_storagePath, fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await content.CopyToAsync(fileStream);
            }
            return fileName;
        }

        public async Task<Stream> GetFileAsync(string fileName)
        {
            var filePath = Path.Combine(_storagePath, fileName);
            if (File.Exists(filePath))
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
            return null;
        }

        public async Task DeleteFileAsync(string fileName)
        {
            var filePath = Path.Combine(_storagePath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            await Task.CompletedTask;
        }
    }
}
