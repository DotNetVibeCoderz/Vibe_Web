using System;
using System.ComponentModel.DataAnnotations;

namespace MyHotel.Models
{
    public class Room
    {
        public int Id { get; set; }
        [Required]
        public string Number { get; set; }
        [Required]
        public string Type { get; set; } // Standard, Deluxe, Suite
        public string Status { get; set; } // Available, Occupied, Cleaning, Maintenance
        public decimal Price { get; set; }
        public string Notes { get; set; }
    }

    public class Guest
    {
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Preference { get; set; } 
    }

    public class Reservation
    {
        public int Id { get; set; }
        public int GuestId { get; set; }
        public Guest Guest { get; set; }
        public int RoomId { get; set; }
        public Room Room { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } // Confirmed, CheckedIn, CheckedOut, Cancelled
        public decimal TotalAmount { get; set; }
    }
}