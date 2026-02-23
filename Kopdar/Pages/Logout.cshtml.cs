using System;
using System.Threading.Tasks;
using Kopdar.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Internal;

namespace Kopdar.Pages
{
    public class LogoutModel : PageModel
    {
        AppService appService { set; get; }
        public LogoutModel(AppService svc)
        {
            this.appService = svc;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            // Clear the existing external cookie
            await HttpContext
                .SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            this.appService.Logout();   
            return LocalRedirect(Url.Content("/"));
        }
    }
}
