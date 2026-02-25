using System.ComponentModel.DataAnnotations;

namespace MyResto.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal => Quantity * UnitPrice;
        public string? Note { get; set; } // Special instructions
    }
}