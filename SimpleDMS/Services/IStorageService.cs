using System.IO;
using System.Threading.Tasks;

namespace SimpleDMS.Services
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder);
        Task<Stream> GetFileAsync(string filePath);
        Task DeleteFileAsync(string filePath);
    }

    public class FileSystemStorageService : IStorageService
    {
        private readonly string _rootPath;

        public FileSystemStorageService(string rootPath)
        {
            _rootPath = rootPath;
            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
            }
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder)
        {
            var targetFolder = Path.Combine(_rootPath, folder);
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(targetFolder, uniqueFileName);

            using var ws = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(ws);

            return Path.Combine(folder, uniqueFileName);
        }

        public Task<Stream> GetFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_rootPath, filePath);
            if (!File.Exists(fullPath)) throw new FileNotFoundException();
            return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
        }

        public Task DeleteFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_rootPath, filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }
    }
}