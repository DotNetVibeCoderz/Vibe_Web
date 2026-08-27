namespace Lapak.Models;

/// <summary>
/// Shopping cart item - linked to user session
/// </summary>
public class CartItem : EntityBase
{
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }

    // Foreign Keys
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
