using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using LitePost.Data.Models;
using LitePost.Auth;

namespace LitePost.Data.Services
{
    public class UserService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public UserService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<User?> Login(string username, string password)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            if (VerifyPassword(password, user.PasswordHash))
            {
                return user;
            }
            return null;
        }

        public async Task<bool> Register(string username, string password, string displayName, string email = "")
        {
            using var context = _dbContextFactory.CreateDbContext();
            if (await context.Users.AnyAsync(u => u.Username == username)) return false;
            
            // For now, allow empty email or unique email if provided
            if (!string.IsNullOrEmpty(email) && await context.Users.AnyAsync(u => u.Email == email)) return false;

            var user = new User
            {
                Username = username,
                Email = email, // New field
                PasswordHash = HashPassword(password),
                DisplayName = displayName,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserProfile(int userId, string displayName, string bio, string avatarUrl, string email)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var user = await context.Users.FindAsync(userId);
            if (user == null) return false;
            user.Email = email; // Update email as well
            user.DisplayName = displayName;
            user.Bio = bio;
            user.AvatarUrl = avatarUrl;
            
            await context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> ChangePassword(int userId, string oldPassword, string newPassword)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var user = await context.Users.FindAsync(userId);
            if (user == null) return false;

            if (!VerifyPassword(oldPassword, user.PasswordHash)) return false;

            user.PasswordHash = HashPassword(newPassword);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteAccount(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var user = await context.Users.FindAsync(userId);
            if (user != null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }
        }

        // --- Password Reset Logic ---

        public async Task<string?> GeneratePasswordResetToken(string email)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;

            var token = Guid.NewGuid().ToString();
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiryDate = DateTime.UtcNow.AddHours(1), // 1 hour expiry
                IsUsed = false
            };

            context.PasswordResetTokens.Add(resetToken);
            await context.SaveChangesAsync();
            
            return token;
        }

        public async Task<bool> ValidateResetToken(string token)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var resetToken = await context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow);
            
            return resetToken != null;
        }

        public async Task<bool> ResetPasswordWithToken(string token, string newPassword)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var resetToken = await context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow);
            
            if (resetToken == null) return false;

            var user = resetToken.User;
            user.PasswordHash = HashPassword(newPassword);
            
            resetToken.IsUsed = true;
            
            await context.SaveChangesAsync();
            return true;
        }

        // -----------------------------

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        public async Task<User?> GetUser(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Users
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}