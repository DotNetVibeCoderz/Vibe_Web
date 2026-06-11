namespace VirtualDoctor.Models;

public class DoctorReview
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DoctorId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ConsultationId { get; set; }
    public string? AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Doctor Doctor { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public Consultation? Consultation { get; set; }
    public Appointment? Appointment { get; set; }
}
