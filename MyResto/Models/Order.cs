using System.ComponentModel.DataAnnotations;

namespace MyResto.Models
{
    public enum OrderStatus { Pending, Paid, Completed, Cancelled }
    public enum PaymentMethod { Cash, CreditCard, QRCode, Split }

    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        
        public int? TableId { get; set; }
        public RestaurantTable? Table { get; set; }
        
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}