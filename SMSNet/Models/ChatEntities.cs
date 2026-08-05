using System.ComponentModel.DataAnnotations;

namespace SMSNet.Models;

/// <summary>A conversation thread with the assistant. Owned by exactly one user.</summary>
public class ChatSession
{
    public int Id { get; set; }

    /// <summary>Identity user id. Scoping every query by this is what keeps threads private.</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Title { get; set; } = "Percakapan baru";

    /// <summary>Provider this thread was started on, so the UI can show where the answers came from.</summary>
    [MaxLength(40)]
    public string Provider { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Model { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ChatMessage> Messages { get; set; } = new();
}

public class ChatMessage
{
    public int Id { get; set; }

    public int ChatSessionId { get; set; }

    public ChatSession? ChatSession { get; set; }

    /// <summary>user | assistant | system</summary>
    [Required, MaxLength(20)]
    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;

    /// <summary>Names of the kernel functions used to answer, for the "alat yang dipakai" trace.</summary>
    [MaxLength(500)]
    public string? ToolsUsed { get; set; }

    /// <summary>Set when generation failed, so the thread can show the failure in place.</summary>
    public bool IsError { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<ChatAttachment> Attachments { get; set; } = new();
}

public enum AttachmentKind
{
    Image = 0,
    Document = 1
}

public class ChatAttachment
{
    public int Id { get; set; }

    public int ChatMessageId { get; set; }

    public ChatMessage? ChatMessage { get; set; }

    [Required, MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Public URL under wwwroot (or the configured storage provider).</summary>
    [Required, MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public AttachmentKind Kind { get; set; }
}
