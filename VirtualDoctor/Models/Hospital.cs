// Model rumah sakit / klinik
namespace VirtualDoctor.Models;

public class Hospital
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public HospitalType Type { get; set; } = HospitalType.Hospital;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? Facilities { get; set; } // JSON array
    public double Rating { get; set; } = 4.0;
    public int TotalReviews { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AcceptsInsurance { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

public enum HospitalType { Hospital, Clinic, HealthCenter, Pharmacy }
