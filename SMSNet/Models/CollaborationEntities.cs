using System.ComponentModel.DataAnnotations;

namespace SMSNet.Models;

/// <summary>
/// Discussion threads that hang off another record — a forum topic, a KPI
/// indicator, and anything added later.
/// <para>
/// The thread is addressed by a (<see cref="ThreadType"/>, <see cref="ThreadId"/>)
/// pair rather than a foreign key per host table. A real FK per host would mean a
/// new nullable column and a new migration every time another page wants comments;
/// this way a page opts in by passing two values. The trade is that the database
/// cannot cascade-delete, so hosts delete their own comments explicitly — see
/// <c>CommentService.DeleteThreadAsync</c>.
/// </para>
/// </summary>
public class Comment
{
    public int Id { get; set; }

    /// <summary>Which kind of record this hangs off. See <see cref="CommentThreads"/>.</summary>
    [Required, MaxLength(40)]
    public string ThreadType { get; set; } = string.Empty;

    /// <summary>Id of the host record within <see cref="ThreadType"/>.</summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// Identity user id of the author. Ownership is checked against this, never
    /// against the display name — names are not unique and are user-editable.
    /// </summary>
    [Required, MaxLength(450)]
    public string AuthorUserId { get; set; } = string.Empty;

    /// <summary>Display name captured at post time, so the thread still reads correctly
    /// if the account is later renamed or removed.</summary>
    [Required, MaxLength(160)]
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Plain text, emoji included. Rendered escaped — never as HTML.</summary>
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<CommentAttachment> Attachments { get; set; } = new();
}

public class CommentAttachment
{
    public int Id { get; set; }

    public int CommentId { get; set; }

    public Comment? Comment { get; set; }

    [Required, MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Public URL under wwwroot.</summary>
    [Required, MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    /// <summary>True when the file can be shown inline rather than linked.</summary>
    public bool IsImage { get; set; }
}

/// <summary>The known <see cref="Comment.ThreadType"/> values, in one place so a
/// typo cannot silently orphan a thread.</summary>
public static class CommentThreads
{
    public const string Forum = "forum";
    public const string Performance = "kinerja";
}
