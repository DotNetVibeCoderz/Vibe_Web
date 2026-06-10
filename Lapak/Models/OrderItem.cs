namespace Lapak.Models;

/// <summary>
/// Individual item within an order
/// </summary>
public class OrderItem : EntityBase
{
    public int Quantity { get; set; } = 1;
    public decimal Price { get; set; } // Price at time of purchase
    public decimal SubTotal { get; set; }
    public string? Notes { get; set; }

    // Foreign Keys
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
