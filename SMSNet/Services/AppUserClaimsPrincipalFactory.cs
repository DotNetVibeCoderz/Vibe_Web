using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SMSNet.Models;

namespace SMSNet.Services;

/// <summary>
/// Puts the user's display name into the auth cookie.
/// <para>
/// Without this the layout would have to hit the database on every render just
/// to greet someone by name — once per component, per circuit, per navigation.
/// </para>
/// </summary>
public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    public const string FullNameClaim = "FullName";
    public const string RoleDisplayClaim = "RoleDisplay";

    public AppUserClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            identity.AddClaim(new Claim(FullNameClaim, user.FullName));
        }

        if (!string.IsNullOrWhiteSpace(user.RoleDisplay))
        {
            identity.AddClaim(new Claim(RoleDisplayClaim, user.RoleDisplay));
        }

        return identity;
    }
}
