using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using ERM.Data;
using System.Linq;

namespace ERM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] bool rememberMe = false)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return Redirect("/");
            }
            
            return Redirect("/Identity/Account/Login?error=Invalid login attempt.");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] string fullName, [FromForm] string email, [FromForm] string password, [FromForm] string confirmPassword)
        {
            if (password != confirmPassword)
            {
                return Redirect("/Identity/Account/Register?error=Passwords do not match.");
            }

            var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName };
            var result = await _userManager.CreateAsync(user, password);
            
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Employee");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Redirect("/");
            }

            var errorMsg = result.Errors.FirstOrDefault()?.Description ?? "Registration failed.";
            return Redirect($"/Identity/Account/Register?error={System.Net.WebUtility.UrlEncode(errorMsg)}");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/");
        }
    }
}