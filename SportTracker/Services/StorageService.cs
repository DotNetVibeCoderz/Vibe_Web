using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace SportTracker.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(string fileName, Stream stream);
    }

    public class FileSystemStorageService : IStorageService
    {
        private readonly string _uploadFolder;

        public FileSystemStorageService(IConfiguration config, IWebHostEnvironment env)
        {
            _uploadFolder = Path.Combine(env.WebRootPath, "uploads");
            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        public async Task<string> UploadFileAsync(string fileName, Stream stream)
        {
            var newFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(_uploadFolder, newFileName);
            
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream);

            return $"/uploads/{newFileName}";
        }
    }
}