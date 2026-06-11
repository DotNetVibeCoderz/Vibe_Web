// Model riwayat chat AI
namespace VirtualDoctor.Models;

public class ChatHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = "Konsultasi Baru";
    public string? LlmProvider { get; set; } // OpenAI, Gemini, Anthropic, Ollama
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ApplicationUser User { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ChatHistoryId { get; set; } = string.Empty;
    public string Role { get; set; } = "user"; // user, assistant, system
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public ChatHistory ChatHistory { get; set; } = null!;
}
