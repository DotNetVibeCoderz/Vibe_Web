namespace Lapak.Models;

/// <summary>
/// Store/Seller entity - each seller has one store
/// </summary>
public class Store : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }

    // Contact
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }

    // Verification
    public bool IsVerified { get; set; } = false;
    public DateTime? VerifiedAt { get; set; }
    public string VerificationStatus { get; set; } = "Unverified"; // Unverified, Pending, Verified, Rejected

    // Performance
    public double AverageRating { get; set; } = 0;
    public int RatingCount { get; set; } = 0;
    public int TotalProducts { get; set; } = 0;
    public int TotalSales { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Foreign Key
    public Guid UserId { get; set; }

    // Navigation Properties
    public User User { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<StoreReview> Reviews { get; set; } = new List<StoreReview>();
    public ICollection<StoreLike> Likes { get; set; } = new List<StoreLike>();
}
