using System.ComponentModel.DataAnnotations;

namespace MyResto.Models
{
    public class Ingredient
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Unit { get; set; } = "kg"; // kg, L, pcs
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public DateTime LastRestocked { get; set; } = DateTime.UtcNow;
    }
}