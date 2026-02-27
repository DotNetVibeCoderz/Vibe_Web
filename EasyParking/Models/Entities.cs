namespace EasyParking.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // admin, operator, pelanggan
    public string Email { get; set; } = string.Empty;
}

public class ParkingSlot
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // e.g., A1, B2
    public string Level { get; set; } = string.Empty; // e.g., Level 1, Level 2
    public bool IsOccupied { get; set; }
    public bool IsReserved { get; set; }
    public bool IsEV { get; set; } // EV Charging slot
    public string Type { get; set; } = "Car"; // Car, Bike
}

public class ParkingTransaction
{
    public int Id { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public int? ParkingSlotId { get; set; }
    public ParkingSlot? ParkingSlot { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal? Amount { get; set; }
    public string Status { get; set; } = "Active"; // Active, Completed
}
