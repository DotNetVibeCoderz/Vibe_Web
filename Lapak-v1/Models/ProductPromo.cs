namespace Lapak.Models;

/// <summary>
/// Product promotion (time-limited discount on product)
/// </summary>
public class ProductPromo : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Discount"; // Discount, Cashback, BuyOneGetOne
    public decimal Value { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsActive { get; set; } = true;

    // Foreign Key
    public Guid ProductId { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
}
