using System.ComponentModel.DataAnnotations;

namespace MyResto.Models
{
    public class Customer
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public int LoyaltyPoints { get; set; }
        public DateTime? LastVisit { get; set; }
    }
}