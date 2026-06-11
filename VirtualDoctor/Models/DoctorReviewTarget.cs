namespace VirtualDoctor.Models;

public class DoctorReviewTarget
{
    public string SourceId { get; set; } = string.Empty;
    public ReviewSourceType SourceType { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum ReviewSourceType
{
    Consultation,
    Appointment
}
