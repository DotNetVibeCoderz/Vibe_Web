using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SMSNet.Models;

namespace SMSNet.Controllers;

[ApiController]
public class AccountController : ControllerBase
{
    private readonly SignInManager<AppUser> _signInManager;

    public AccountController(SignInManager<AppUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [IgnoreAntiforgeryToken]
    [HttpPost("/account/login")]
    public async Task<IActionResult> Login([FromForm] LoginRequest request, [FromQuery] string? returnUrl = "/")
    {
        var result = await _signInManager.PasswordSignInAsync(request.UserName, request.Password, request.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        return Redirect($"/auth/login?error=1&ReturnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    [HttpPost("/account/logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/auth/login");
    }

    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
