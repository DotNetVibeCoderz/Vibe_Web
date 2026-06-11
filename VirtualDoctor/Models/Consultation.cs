// Model untuk konsultasi
namespace VirtualDoctor.Models;

public class Consultation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public ConsultationType Type { get; set; } = ConsultationType.Chat;
    public ConsultationStatus Status { get; set; } = ConsultationStatus.Waiting;
    public string? ChiefComplaint { get; set; } // Keluhan utama
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }
    public string? Prescription { get; set; }
    public decimal Fee { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    
    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public ICollection<ConsultationMessage> Messages { get; set; } = new List<ConsultationMessage>();
}

public enum ConsultationType { Chat, Phone, Video }
public enum ConsultationStatus { Waiting, InProgress, Completed, Cancelled }
