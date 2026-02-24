using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;

namespace MyPoS.Services
{
    public class FileSystemStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly StorageConfig _config;

        public FileSystemStorageService(IWebHostEnvironment env, IOptions<StorageConfig> config)
        {
            _env = env;
            _config = config.Value;
        }

        public async Task<string> UploadFileAsync(IBrowserFile file, string fileName)
        {
            // Fallback for upload dir
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(uploads, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.OpenReadStream(10485760).CopyToAsync(fileStream); // max 10MB
            }

            return _config.BaseUrl + uniqueFileName;
        }

        public Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return Task.CompletedTask;

            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
    }
}
