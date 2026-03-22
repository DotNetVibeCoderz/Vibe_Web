using Microsoft.AspNetCore.Identity;

namespace FamilyTree.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public DateTime? BirthDate { get; set; }
}
