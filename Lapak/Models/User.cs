using Microsoft.AspNetCore.Identity;

namespace Lapak.Models;

/// <summary>
/// Extended Application User with e-commerce specific properties
/// </summary>
public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? PhoneNumber2 { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string UserType { get; set; } = "Buyer"; // Buyer, Seller, Admin
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Customer Scoring
    public int Score { get; set; } = 0;
    public string Tier { get; set; } = "Bronze"; // Bronze, Silver, Gold, Platinum
    public int TotalTransactions { get; set; } = 0;
    public decimal TotalTransactionValue { get; set; } = 0;

    // Loyalty Points
    public int LoyaltyPoints { get; set; } = 0;

    // Navigation Properties
    public Store? Store { get; set; }
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
