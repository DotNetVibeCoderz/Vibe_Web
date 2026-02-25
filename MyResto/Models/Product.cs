using System.ComponentModel.DataAnnotations;

namespace MyResto.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public int StockQuantity { get; set; }
    }
}