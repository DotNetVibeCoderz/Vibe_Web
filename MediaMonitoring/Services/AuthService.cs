using System.Security.Cryptography;
using System.Text;
using MediaMonitoring.Data;
using MediaMonitoring.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace MediaMonitoring.Services
{
    public class AuthService
    {
        private readonly MediaMonitoringContext _context;
        private static int? _currentUserId;

        public AuthService(MediaMonitoringContext context)
        {
            _context = context;
        }

        public static int? CurrentUserId => _currentUserId;

        public async Task<(bool Success, string Message)> RegisterAsync(RegisterModel model)
        {
            if (await _context.ApplicationUsers.AnyAsync(u => u.Username == model.Username || u.Email == model.Email))
                return (false, "Username or email already exists");

            var salt = GenerateSalt();
            var passwordHash = HashPassword(model.Password, salt);

            var user = new ApplicationUser
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = passwordHash,
                Salt = salt,
                Role = "User",
                FullName = model.FullName,
                Organization = model.Organization,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ApplicationUsers.Add(user);
            await _context.SaveChangesAsync();

            await LogAuditAsync(user.Id, "Register", "Authentication", null, "New user registered");
            return (true, "Registration successful!");
        }

        public async Task<(bool Success, ApplicationUser? User, string Message)> LoginAsync(string usernameOrEmail, string password)
        {
            var user = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => 
                    (u.Username == usernameOrEmail || u.Email == usernameOrEmail) && u.IsActive);

            if (user == null)
                return (false, null, "User not found");

            var passwordHash = HashPassword(password, user.Salt);
            if (passwordHash != user.PasswordHash)
                return (false, null, "Invalid password");

            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _currentUserId = user.Id;
            await LogAuditAsync(user.Id, "Login", "Authentication", null, "User logged in");

            return (true, user, "Login successful");
        }

        public async Task LogoutAsync()
        {
            if (_currentUserId.HasValue)
            {
                await LogAuditAsync(_currentUserId.Value, "Logout", "Authentication", null, "User logged out");
                _currentUserId = null;
            }
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordModel model)
        {
            var user = await _context.ApplicationUsers.FindAsync(userId);
            if (user == null)
                return (false, "User not found");

            var currentPasswordHash = HashPassword(model.CurrentPassword, user.Salt);
            if (currentPasswordHash != user.PasswordHash)
                return (false, "Current password is incorrect");

            var newSalt = GenerateSalt();
            var newPasswordHash = HashPassword(model.NewPassword, newSalt);

            user.PasswordHash = newPasswordHash;
            user.Salt = newSalt;
            await _context.SaveChangesAsync();

            await LogAuditAsync(userId, "ChangePassword", "Authentication", null, "Password changed");
            return (true, "Password changed successfully!");
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(int userId)
        {
            return await _context.ApplicationUsers.FindAsync(userId);
        }

        public async Task<UserProfileModel?> GetUserProfileAsync(int userId)
        {
            var user = await _context.ApplicationUsers.FindAsync(userId);
            if (user == null) return null;

            return new UserProfileModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Organization = user.Organization,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            };
        }

        public async Task UpdateUserProfileAsync(int userId, string? fullName, string? organization)
        {
            var user = await _context.ApplicationUsers.FindAsync(userId);
            if (user != null)
            {
                user.FullName = fullName;
                user.Organization = organization;
                await _context.SaveChangesAsync();
                await LogAuditAsync(userId, "UpdateProfile", "User", userId, "Profile updated");
            }
        }

        public bool HasPermission(string role, string permission)
        {
            return role switch
            {
                "Admin" => true,
                "Analyst" => permission is "View" or "Export" or "Analyze" or "Configure",
                "Viewer" => permission is "View" or "Export",
                _ => false
            };
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _context.ApplicationUsers.OrderByDescending(u => u.CreatedAt).ToListAsync();
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, string newRole)
        {
            var user = await _context.ApplicationUsers.FindAsync(userId);
            if (user == null) return false;

            user.Role = newRole;
            await _context.SaveChangesAsync();
            await LogAuditAsync(null, "UpdateRole", "User", userId, $"Role changed to {newRole}");
            return true;
        }

        public async Task LogAuditAsync(int? userId, string action, string entityType, int? entityId, string details, string ipAddress = "", string userAgent = "")
        {
            var audit = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetRecentAuditLogsAsync(int count = 100)
        {
            return await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        private static string GenerateSalt()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = string.Concat(password, salt);
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashBytes);
        }

        public async Task InitializeDefaultAdminAsync()
        {
            if (!await _context.ApplicationUsers.AnyAsync())
            {
                var registerModel = new RegisterModel
                {
                    Username = "admin",
                    Email = "admin@localhost.com",
                    Password = "admin123",
                    ConfirmPassword = "admin123"
                };
                await RegisterAsync(registerModel);
                
                var admin = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Username == "admin");
                if (admin != null)
                {
                    admin.Role = "Admin";
                    await _context.SaveChangesAsync();
                }
                
                Console.WriteLine("Default admin user created: admin / admin123");
            }
        }
    }
}
