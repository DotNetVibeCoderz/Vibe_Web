namespace Lapak.Models;

/// <summary>
/// Shipping tracking history
/// </summary>
public class ShippingTracking : EntityBase
{
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime EventDate { get; set; } = DateTime.UtcNow;

    // Foreign Key
    public Guid OrderId { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
