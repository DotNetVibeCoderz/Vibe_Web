// Model pesan konsultasi
namespace VirtualDoctor.Models;

public class ConsultationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConsultationId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? AttachmentUrl { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public Consultation Consultation { get; set; } = null!;
}

public enum MessageType { Text, Image, File, Video, System }
