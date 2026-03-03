using SportTracker.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace SportTracker.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

        public void MarkUserAsAuthenticated(User user)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("ProfileImage", user.ProfileImageUrl ?? "")
            }, "apiauth_type");

            _currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void MarkUserAsLoggedOut()
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        public User? GetCurrentUser()
        {
            var idClaim = _currentUser.FindFirst(ClaimTypes.NameIdentifier);
            if(idClaim != null && int.TryParse(idClaim.Value, out int id))
            {
                return new User
                {
                    Id = id,
                    Username = _currentUser.Identity?.Name ?? "",
                    Email = _currentUser.FindFirst(ClaimTypes.Email)?.Value ?? "",
                    Role = _currentUser.FindFirst(ClaimTypes.Role)?.Value ?? "",
                    ProfileImageUrl = _currentUser.FindFirst("ProfileImage")?.Value ?? ""
                };
            }
            return null;
        }
    }
}