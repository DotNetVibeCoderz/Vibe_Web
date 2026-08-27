using Lapak.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Lapak.Controllers;

/// <summary>
/// Account endpoints for register/login using standard HTTP request
/// so Set-Cookie can be written before response starts.
/// </summary>
[ApiController]
[Route("api/account")]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public record RegisterRequest(
        string FullName,
        string Email,
        string Phone,
        string Password,
        string ConfirmPassword,
        string UserType);

    public record LoginRequest(
        string Email,
        string Password,
        bool RememberMe,
        string? Redirect);

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request)
    {
        var validationError = ValidateRegister(request);
        if (!string.IsNullOrWhiteSpace(validationError))
            return Redirect($"/account/register?error={Uri.EscapeDataString(validationError)}");

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.Phone,
            UserType = request.UserType
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var error = string.Join(", ", result.Errors.Select(e => e.Description));
            return Redirect($"/account/register?error={Uri.EscapeDataString(error)}");
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        var target = request.UserType == "Seller" ? "/seller/register-store" : "/";
        return Redirect(target);
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Redirect($"/account/login?error={Uri.EscapeDataString("Email dan password harus diisi.")}");

        var result = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var target = string.IsNullOrWhiteSpace(request.Redirect) ? "/" : request.Redirect;
            return Redirect(target);
        }

        if (result.IsLockedOut)
            return Redirect($"/account/login?error={Uri.EscapeDataString("Akun terkunci.")}");

        return Redirect($"/account/login?error={Uri.EscapeDataString("Email atau password salah.")}");
    }

    /// <summary>
    /// Re-issues the auth cookie so claims changed during the session take effect.
    /// </summary>
    /// <remarks>
    /// Role claims are baked into the cookie at sign-in. When a buyer opens a shop
    /// mid-session their UserType becomes "Seller" in the database, but the cookie
    /// still says "Buyer" — and a Blazor circuit cannot write a cookie. Bouncing
    /// through this endpoint refreshes it, then returns the user where they were going.
    /// </remarks>
    [HttpGet("/account/refresh")]
    public async Task<IActionResult> Refresh([FromQuery] string? redirect)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null) await _signInManager.RefreshSignInAsync(user);

        // Only ever bounce back inside this site.
        var target = !string.IsNullOrWhiteSpace(redirect) && Url.IsLocalUrl(redirect) ? redirect : "/";
        return Redirect(target);
    }

    /// <summary>
    /// Signs the user out. Reached from the account menu as a plain link, so it
    /// answers GET as well as POST — the cookie can only be cleared on a real
    /// request, never from inside a Blazor circuit.
    /// </summary>
    [HttpGet("/account/logout")]
    [HttpPost("/account/logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/");
    }

    private static string ValidateRegister(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return "Nama harus diisi.";
        if (string.IsNullOrWhiteSpace(request.Email)) return "Email harus diisi.";
        if (request.Password.Length < 8) return "Password minimal 8 karakter.";
        if (request.Password != request.ConfirmPassword) return "Password tidak cocok.";
        if (request.UserType is not ("Buyer" or "Seller")) return "Tipe akun tidak valid.";
        return string.Empty;
    }
}
