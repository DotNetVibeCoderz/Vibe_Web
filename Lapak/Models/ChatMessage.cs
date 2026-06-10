namespace Lapak.Models;

/// <summary>
/// Chat message for AI chatbots (Tony Kurus & Siti Bohay)
/// </summary>
public class ChatMessage : EntityBase
{
    public string ChatBotType { get; set; } = "TonyKurus"; // TonyKurus, SitiBohay
    public string Role { get; set; } = "User"; // User, Assistant, System
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrlsJson { get; set; }
    public string? MetadataJson { get; set; } // For tool calls, LLM provider info, etc.
    public string? LlmProvider { get; set; }

    // Foreign Key
    public Guid UserId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
