using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyPoS.Pages
{
    /// <summary>
    /// Halaman yang dirujuk UseExceptionHandler. Sebelumnya Program.cs menunjuk ke "/Error"
    /// yang tidak pernah dibuat, sehingga kesalahan di lingkungan produksi berujung pada
    /// 404 alih-alih pesan yang bisa dibaca pengguna.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public void OnGet() => RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
