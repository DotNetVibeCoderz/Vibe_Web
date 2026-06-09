using Microsoft.AspNetCore.Components.Forms;

namespace SimpleBidding.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IBrowserFile file);
        Task DeleteFileAsync(string fileUrl);
    }

    public class FileStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _env;

        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> UploadFileAsync(IBrowserFile file)
        {
            // Pastikan wwwroot ada
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folderPath = Path.Combine(webRoot, "uploads");
            
            if (!Directory.Exists(folderPath)) 
                Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
            var filePath = Path.Combine(folderPath, fileName);

            // Gunakan limit yang cukup besar (10MB)
            using var stream = file.OpenReadStream(10 * 1024 * 1024);
            using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream);

            // Kembalikan path relatif yang bisa diakses browser
            return $"/uploads/{fileName}";
        }

        public Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl) || !fileUrl.StartsWith("/uploads/"))
                return Task.CompletedTask;

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(webRoot, "uploads", fileName);
            
            if (File.Exists(filePath)) 
                File.Delete(filePath);
                
            return Task.CompletedTask;
        }
    }

    public class AzureBlobStorageService : IStorageService
    {
        public AzureBlobStorageService(IConfiguration config) { }
        public async Task<string> UploadFileAsync(IBrowserFile file) => $"https://azure-simulated.com/{file.Name}";
        public Task DeleteFileAsync(string fileUrl) => Task.CompletedTask;
    }

    public class S3StorageService : IStorageService
    {
        public S3StorageService(IConfiguration config) { }
        public async Task<string> UploadFileAsync(IBrowserFile file) => $"https://s3-simulated.com/{file.Name}";
        public Task DeleteFileAsync(string fileUrl) => Task.CompletedTask;
    }
}
