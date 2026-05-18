using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace SimpleBidding.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        public void OnGet() { }
        public IActionResult OnPost()
        {
            // Simulated success
            return Content("<div class='container mt-5 text-center'><h3>Reset link sent!</h3><p>Check your email (simulated). <a href='/Account/Login'>Back to Login</a></p></div>", "text/html");
        }
    }
}
