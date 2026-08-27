using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace MyPoS.Services
{
    /// <summary>Isi sesi yang dititipkan ke browser dalam bentuk terenkripsi.</summary>
    public record UserSession(string Username, string Role, string? FullName, DateTime ExpiresAt);

    /// <summary>
    /// Menyimpan sesi di ProtectedLocalStorage, bukan hanya di field circuit.
    ///
    /// Sebelumnya identitas hanya hidup di memori satu circuit Blazor, sehingga kasir
    /// terlempar ke halaman login setiap kali me-refresh halaman atau koneksi terputus
    /// sesaat - masalah nyata di konter yang jaringannya tidak stabil.
    /// </summary>
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private const string StorageKey = "mypos.session";
        private static readonly AuthenticationState Anonymous =
            new(new ClaimsPrincipal(new ClaimsIdentity()));

        private readonly ProtectedLocalStorage _storage;
        private readonly SettingsService _settings;
        private ClaimsPrincipal? _currentUser;

        public CustomAuthStateProvider(ProtectedLocalStorage storage, SettingsService settings)
        {
            _storage = storage;
            _settings = settings;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_currentUser is not null)
                return new AuthenticationState(_currentUser);

            try
            {
                var stored = await _storage.GetAsync<UserSession>(StorageKey);
                if (stored.Success && stored.Value is UserSession session)
                {
                    if (session.ExpiresAt > DateTime.UtcNow)
                    {
                        _currentUser = BuildPrincipal(session);
                        return new AuthenticationState(_currentUser);
                    }

                    await _storage.DeleteAsync(StorageKey);
                }
            }
            catch (Exception)
            {
                // JS interop belum tersedia (mis. saat prerender) atau data rusak:
                // perlakukan sebagai belum login, bukan sebagai kegagalan aplikasi.
            }

            return Anonymous;
        }

        public async Task SignInAsync(string username, string role, string? fullName)
        {
            var hours = Math.Clamp(_settings.Current.SessionTimeoutHours, 1, 24 * 30);
            var session = new UserSession(username, role, fullName, DateTime.UtcNow.AddHours(hours));

            _currentUser = BuildPrincipal(session);

            try
            {
                await _storage.SetAsync(StorageKey, session);
            }
            catch (Exception)
            {
                // Tanpa penyimpanan browser, sesi tetap berlaku selama circuit ini hidup.
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        public async Task SignOutAsync()
        {
            _currentUser = null;

            try
            {
                await _storage.DeleteAsync(StorageKey);
            }
            catch (Exception)
            {
                // diabaikan: keluar dari sesi tidak boleh gagal karena masalah penyimpanan
            }

            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        }

        private static ClaimsPrincipal BuildPrincipal(UserSession session)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, session.Username),
                new Claim(ClaimTypes.Role, session.Role),
                new Claim("FullName", session.FullName ?? session.Username)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "MyPoSAuth"));
        }
    }
}
