using Microsoft.AspNetCore.Identity;

namespace FMSNet.Data
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Operator";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Vehicle
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Truck"; // Truck, Van, Car
        public string LicensePlate { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // Active, Maintenance, Idle
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double FuelLevel { get; set; } // Percentage
        public double Odometer { get; set; } // Km
        public int? DriverId { get; set; }
        public virtual Driver? Driver { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    }

    public class Driver
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, OnDuty, OffDuty
        public double SafetyScore { get; set; } = 100.0; // 0-100 based on behavior
        public DateTime HireDate { get; set; } = DateTime.UtcNow;
    }

    public class MaintenanceRecord
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public virtual Vehicle? Vehicle { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Pending
    }

    public class FuelLog
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public virtual Vehicle? Vehicle { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public double AmountLiters { get; set; }
        public decimal Cost { get; set; }
        public string Location { get; set; } = string.Empty;
    }
    
    public class ApiKey
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}