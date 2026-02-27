using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace AISTracker.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Vessel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string MMSI { get; set; }
        [Required]
        public string Name { get; set; }
        public string IMONumber { get; set; }
        public string Flag { get; set; }
        public string VesselType { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public string Status { get; set; } // Active, Inactive, Maintenance
        
        public ICollection<PositionReport> PositionReports { get; set; }
    }

    public class PositionReport
    {
        [Key]
        public int Id { get; set; }
        public int VesselId { get; set; }
        public Vessel Vessel { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double SpeedOverGround { get; set; } // Knots
        public double CourseOverGround { get; set; } // Degrees
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string NavigationalStatus { get; set; } // Under way using engine, At anchor, etc.
    }

    public class Alert
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; } // Info, Warning, Critical
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
    }

    public class Port
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string UNLocCode { get; set; }
    }
}
