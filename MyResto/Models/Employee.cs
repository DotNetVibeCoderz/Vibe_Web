using System.ComponentModel.DataAnnotations;

namespace MyResto.Models
{
    public class Employee
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = string.Empty; // Manager, Chef, Waiter, Cashier
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}