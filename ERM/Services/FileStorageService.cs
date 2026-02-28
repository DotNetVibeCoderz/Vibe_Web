using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ERM.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(string fileName, byte[] fileData);
        Task<byte[]> GetFileAsync(string filePath);
    }

    public class FileSystemStorageService : IFileStorageService
    {
        private readonly string _storagePath;

        public FileSystemStorageService(IConfiguration config)
        {
            var path = config["Storage:Path"] ?? "UploadedFiles";
            _storagePath = Path.Combine(Directory.GetCurrentDirectory(), path);
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public async Task<string> SaveFileAsync(string fileName, byte[] fileData)
        {
            var uniqueName = $"{Guid.NewGuid()}_{fileName}";
            var path = Path.Combine(_storagePath, uniqueName);
            await File.WriteAllBytesAsync(path, fileData);
            return uniqueName;
        }

        public async Task<byte[]> GetFileAsync(string fileName)
        {
            var path = Path.Combine(_storagePath, fileName);
            if (File.Exists(path))
            {
                return await File.ReadAllBytesAsync(path);
            }
            return null;
        }
    }
}