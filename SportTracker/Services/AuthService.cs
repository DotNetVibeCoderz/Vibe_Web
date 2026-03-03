using SportTracker.Data;
using SportTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace SportTracker.Services
{
    public class AuthService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AuthService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            using var db = await _factory.CreateDbContextAsync();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            if (user.PasswordHash == AppDbContext.HashPassword(password))
            {
                return user;
            }
            return null;
        }

        public async Task<bool> RegisterAsync(string username, string password, string email)
        {
            using var db = await _factory.CreateDbContextAsync();
            if (await db.Users.AnyAsync(u => u.Username == username)) return false;

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = AppDbContext.HashPassword(password),
                Role = "user",
                ProfileImageUrl = $"https://ui-avatars.com/api/?name={username}&background=random"
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string username, string newPassword)
        {
            using var db = await _factory.CreateDbContextAsync();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;

            user.PasswordHash = AppDbContext.HashPassword(newPassword);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProfileAsync(int userId, string email, string imageUrl)
        {
            using var db = await _factory.CreateDbContextAsync();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.Email = email;
            if(!string.IsNullOrEmpty(imageUrl)) user.ProfileImageUrl = imageUrl;
            await db.SaveChangesAsync();
            return true;
        }
    }
}