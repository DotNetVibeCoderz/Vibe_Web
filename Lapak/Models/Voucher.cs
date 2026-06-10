namespace Lapak.Models;

/// <summary>
/// Voucher/Promo code for discounts
/// </summary>
public class Voucher : EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Percentage"; // Percentage, Fixed, Shipping
    public decimal Value { get; set; } // Percentage value or fixed amount
    public decimal? MaxDiscount { get; set; } // Cap for percentage discounts
    public decimal? MinPurchase { get; set; } // Minimum purchase required

    // Validity
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);
    public int MaxUsage { get; set; } = 100;
    public int CurrentUsage { get; set; } = 0;
    public int MaxUsagePerUser { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    // Target
    public string? TargetTier { get; set; } // Bronze, Silver, Gold, Platinum, null = all
    public string? TargetCategoryIdsJson { get; set; }
    public string? TargetProductIdsJson { get; set; }

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
