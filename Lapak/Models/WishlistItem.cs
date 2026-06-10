namespace Lapak.Models;

/// <summary>
/// Wishlist item for buyers
/// </summary>
public class WishlistItem : EntityBase
{
    public string? Notes { get; set; }

    // Foreign Keys
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
