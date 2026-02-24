using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyPoS.Data;
using Microsoft.AspNetCore.Components.Authorization;

namespace MyPoS.Services
{
    public class AuthService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(IDbContextFactory<AppDbContext> dbContextFactory, AuthenticationStateProvider authStateProvider)
        {
            _dbContextFactory = dbContextFactory;
            _authStateProvider = authStateProvider;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            // Simple Base64 for demo purposes
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
            
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == encodedPassword);
            
            if (user != null)
            {
                var customProvider = (CustomAuthStateProvider)_authStateProvider;
                customProvider.MarkUserAsAuthenticated(user.Username, user.Role);
                return true;
            }
            return false;
        }

        public void Logout()
        {
            var customProvider = (CustomAuthStateProvider)_authStateProvider;
            customProvider.MarkUserAsLoggedOut();
        }
    }
}
