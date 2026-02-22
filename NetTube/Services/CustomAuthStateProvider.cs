using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using NetTube.Models;
using NetTube.Data;
using Microsoft.EntityFrameworkCore;

namespace NetTube.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        private ClaimsPrincipal? _currentUser;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public User? CurrentUserModel { get; private set; }

        public CustomAuthStateProvider(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var principal = _currentUser ?? _anonymous;
            return Task.FromResult(new AuthenticationState(principal));
        }

        public async Task<bool> LoginAsync(string usernameOrEmail, string password)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.FirstOrDefaultAsync(u => 
                (u.Username == usernameOrEmail || u.Email == usernameOrEmail) && u.PasswordHash == password);

            if (user != null)
            {
                CurrentUserModel = user;
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Uri, user.ProfilePictureUrl ?? "")
                }, "CustomAuth");

                _currentUser = new ClaimsPrincipal(identity);
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return true;
            }
            return false;
        }

        public void Logout()
        {
            _currentUser = null;
            CurrentUserModel = null;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task<bool> RegisterAsync(User newUser)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            if (await context.Users.AnyAsync(u => u.Username == newUser.Username || u.Email == newUser.Email))
                return false;

            newUser.ProfilePictureUrl = "https://i.pravatar.cc/150?u=" + newUser.Username;
            context.Users.Add(newUser);
            await context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> ChangePasswordAsync(string email, string newPassword)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;
            
            user.PasswordHash = newPassword;
            await context.SaveChangesAsync();
            return true;
        }
    }
}