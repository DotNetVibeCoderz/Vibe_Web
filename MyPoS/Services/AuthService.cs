using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MyPoS.Data;

namespace MyPoS.Services
{
    public record LoginResult(bool Success, string? Error = null);

    public class AuthService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(IDbContextFactory<AppDbContext> dbContextFactory, AuthenticationStateProvider authStateProvider)
        {
            _dbContextFactory = dbContextFactory;
            _authStateProvider = authStateProvider;
        }

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new LoginResult(false, "Nama pengguna dan kata sandi wajib diisi.");

            using var db = await _dbContextFactory.CreateDbContextAsync();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username.Trim());

            // Pesan yang sama untuk pengguna tidak ada maupun sandi salah, supaya nama
            // pengguna yang valid tidak bisa ditebak dari perbedaan pesan.
            if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
                return new LoginResult(false, "Nama pengguna atau kata sandi salah.");

            if (!user.IsActive)
                return new LoginResult(false, "Akun ini sedang dinonaktifkan.");

            // Baris yang masih memakai penyandian Base64 lama ditulis ulang saat login berhasil.
            if (PasswordHasher.NeedsUpgrade(user.PasswordHash))
            {
                user.PasswordHash = PasswordHasher.Hash(password);
                await db.SaveChangesAsync();
            }

            await ((CustomAuthStateProvider)_authStateProvider).SignInAsync(user.Username, user.Role, user.FullName);
            return new LoginResult(true);
        }

        public Task LogoutAsync()
            => ((CustomAuthStateProvider)_authStateProvider).SignOutAsync();
    }
}
