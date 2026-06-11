// Model untuk pengguna aplikasi
namespace VirtualDoctor.Models;

public class ApplicationUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; } // Male, Female, Other
    public string? BloodType { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Doctor profile mapping
    public bool IsDoctor { get; set; }
    public string? DoctorId { get; set; }

    // Navigation
    public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<ChatHistory> ChatHistories { get; set; } = new List<ChatHistory>();
    public ICollection<DoctorReview> DoctorReviews { get; set; } = new List<DoctorReview>();
}
