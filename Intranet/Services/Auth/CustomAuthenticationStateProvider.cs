using System.Security.Claims;
using Intranet.Data;
using Intranet.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Intranet.Services.Auth;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    private readonly IServiceScopeFactory _scopeFactory;
    private User? _currentUser;

    public CustomAuthenticationStateProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser == null)
            return new AuthenticationState(_anonymous);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, _currentUser.Username),
            new Claim(ClaimTypes.Role, _currentUser.Role),
            new Claim("FullName", _currentUser.FullName),
            new Claim("AvatarUrl", _currentUser.AvatarUrl)
        };

        var identity = new ClaimsIdentity(claims, "CustomAuth");
        var principal = new ClaimsPrincipal(identity);

        return new AuthenticationState(principal);
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password && u.IsActive);
        if (user != null)
        {
            _currentUser = user;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return true;
        }
        return false;
    }

    public void Logout()
    {
        _currentUser = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public User? GetCurrentUser() => _currentUser;
}
