using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SMSNet.Models;

namespace SMSNet.Controllers;

/// <summary>
/// Sign-in and sign-out.
/// <para>
/// These live in a controller rather than a component because an interactive
/// Blazor circuit cannot write the authentication cookie — the response headers
/// are long gone by the time the circuit runs.
/// </para>
/// </summary>
[ApiController]
[AllowAnonymous]
public class AccountController : ControllerBase
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(SignInManager<AppUser> signInManager, ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpPost("/account/login")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginRequest request, [FromForm] string? returnUrl = null)
    {
        var destination = SafeReturnUrl(returnUrl);

        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return RedirectToLogin(destination, "error=1");
        }

        var result = await _signInManager.PasswordSignInAsync(
            request.UserName,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserName} signed in", request.UserName);
            return LocalRedirect(destination);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account {UserName} is locked out", request.UserName);
            return RedirectToLogin(destination, "error=locked");
        }

        // Deliberately vague: distinguishing "no such user" from "wrong password"
        // turns the form into a username oracle.
        _logger.LogWarning("Failed sign-in attempt for {UserName}", request.UserName);
        return RedirectToLogin(destination, "error=1");
    }

    /// <summary>
    /// Signs out. The antiforgery token is validated — without it any page on the
    /// internet could log a user out with a hidden auto-submitting form.
    /// </summary>
    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/auth/login?loggedOut=1");
    }

    /// <summary>
    /// Only same-site relative paths are honoured. An attacker-supplied absolute
    /// URL here would make the login form an open redirect.
    /// </summary>
    private string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        return Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
    }

    private IActionResult RedirectToLogin(string returnUrl, string query) =>
        Redirect($"/auth/login?{query}&ReturnUrl={Uri.EscapeDataString(returnUrl)}");

    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
