using Microsoft.EntityFrameworkCore;
using QueueKiosk.Data;
using QueueKiosk.Models;

namespace QueueKiosk.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly CustomAuthStateProvider _authStateProvider;

    public AuthService(AppDbContext db, CustomAuthStateProvider authStateProvider)
    {
        _db = db;
        _authStateProvider = authStateProvider;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);
        if (user != null)
        {
            _authStateProvider.MarkUserAsAuthenticated(user.Username, user.Role);
            return true;
        }
        return false;
    }

    public void Logout()
    {
        _authStateProvider.MarkUserAsLoggedOut();
    }
}
