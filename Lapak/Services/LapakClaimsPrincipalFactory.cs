using System.Security.Claims;
using Lapak.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Lapak.Services;

/// <summary>
/// Projects <see cref="User.UserType"/> onto the standard role claim.
/// </summary>
/// <remarks>
/// The app stores who someone is on the user row ("Buyer" / "Seller" / "Admin")
/// rather than in AspNetRoles. Emitting it as a role claim at sign-in is what
/// lets pages guard themselves with <c>[Authorize(Roles = "Admin")]</c> instead
/// of re-querying the database and redirecting by hand.
/// </remarks>
public class LapakClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, IdentityRole<Guid>>
{
    public LapakClaimsPrincipalFactory(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.UserType))
            identity.AddClaim(new Claim(ClaimTypes.Role, user.UserType));

        // A seller is also a buyer: they shop from the same cart.
        if (user.UserType is "Seller" or "Admin")
            identity.AddClaim(new Claim(ClaimTypes.Role, "Buyer"));

        identity.AddClaim(new Claim("tier", user.Tier));
        identity.AddClaim(new Claim("fullName", user.FullName));

        return identity;
    }
}
