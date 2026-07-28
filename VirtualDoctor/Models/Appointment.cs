// Model appointment / booking
namespace VirtualDoctor.Models;

public class Appointment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string? HospitalId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public AppointmentType Type { get; set; } = AppointmentType.InPerson;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsuranceNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Video conference untuk appointment tipe Online
    public string? MeetingProvider { get; set; }
    public string? MeetingId { get; set; }
    public string? MeetingUrl { get; set; }
    public string? MeetingHostUrl { get; set; }
    public string? MeetingPassword { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public Hospital? Hospital { get; set; }
}

public enum AppointmentType { InPerson, Online }
public enum AppointmentStatus { Scheduled, Confirmed, InProgress, Completed, Cancelled }
