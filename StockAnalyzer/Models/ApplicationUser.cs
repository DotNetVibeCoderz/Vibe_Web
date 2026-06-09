using Microsoft.AspNetCore.Identity;

namespace StockAnalyzer.Models;

/// <summary>
/// Application user model extending ASP.NET Core Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>User display name</summary>
    [PersonalData]
    public string? DisplayName { get; set; }

    /// <summary>Date when user registered</summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last login timestamp</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>User's preferred theme (dark/light)</summary>
    [PersonalData]
    public string PreferredTheme { get; set; } = "light";

    /// <summary>Profile picture URL or path</summary>
    public string? ProfilePictureUrl { get; set; }
}
