// Model untuk dokter
namespace VirtualDoctor.Models;

public class Doctor
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string Specialization { get; set; } = string.Empty; // Umum, Spesialis, Psikolog
    public string? SubSpecialization { get; set; }
    public string? LicenseNumber { get; set; } // STR
    public int ExperienceYears { get; set; }
    public string? About { get; set; }
    public string? Education { get; set; }
    public string? HospitalAffiliation { get; set; }
    public decimal ConsultationFee { get; set; }
    public double Rating { get; set; } = 4.5;
    public int TotalPatients { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsOnline { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
    public ICollection<DoctorReview> Reviews { get; set; } = new List<DoctorReview>();
}
