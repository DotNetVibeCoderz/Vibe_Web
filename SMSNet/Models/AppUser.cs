using Microsoft.AspNetCore.Identity;

namespace SMSNet.Models;

// Custom user entity for identity
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? RoleDisplay { get; set; }
}
