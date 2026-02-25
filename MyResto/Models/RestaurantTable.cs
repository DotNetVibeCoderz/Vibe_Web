using System.ComponentModel.DataAnnotations;

namespace MyResto.Models
{
    public enum TableStatus { Available, Occupied, Reserved, Cleaning }

    public class RestaurantTable
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public TableStatus Status { get; set; } = TableStatus.Available;
    }
}