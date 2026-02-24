using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace MyPoS.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IBrowserFile file, string fileName);
        Task DeleteFileAsync(string fileUrl);
    }
}
