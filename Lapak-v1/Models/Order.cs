namespace Lapak.Models;

/// <summary>
/// Order/Transaction entity
/// </summary>
public class Order : EntityBase
{
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Paid, Processing, Shipped, Delivered, Completed, Cancelled, Refunded
    public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Paid, Refunded, PartialRefund
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentGateway { get; set; }
    public string? PaymentTransactionId { get; set; }

    // Financials
    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }

    // Shipping
    public string ShippingCourier { get; set; } = string.Empty;
    public string ShippingService { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? ShippingAddress { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingProvince { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingNotes { get; set; }

    // Voucher
    public Guid? VoucherId { get; set; }
    public Voucher? Voucher { get; set; }

    // Timestamps
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Foreign Key
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<ShippingTracking> ShippingTrackings { get; set; } = new List<ShippingTracking>();
}
