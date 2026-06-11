// Model order / pesanan
namespace VirtualDoctor.Models;

public class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string? PharmacyId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? CourierName { get; set; }
    public string? TrackingNumber { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsuranceNumber { get; set; }
    public decimal InsuranceCoverage { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    
    public ApplicationUser User { get; set; } = null!;
    public Hospital? Pharmacy { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OrderId { get; set; } = string.Empty;
    public string MedicineId { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal { get; set; }
    
    public Order Order { get; set; } = null!;
    public Medicine Medicine { get; set; } = null!;
}

public enum OrderStatus { Pending, Confirmed, Processing, Shipped, Delivered, Cancelled }
public enum PaymentMethod { Cash, Transfer, EWallet, Insurance }
public enum PaymentStatus { Unpaid, Paid, Refunded }
