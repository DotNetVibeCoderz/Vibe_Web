using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MyLibrary.Models;

// User untuk autentikasi dan profil
public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

public class Book
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required, MaxLength(150)]
    public string Author { get; set; } = string.Empty;
    [MaxLength(20)]
    public string ISBN { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    [MaxLength(20)]
    public string Type { get; set; } = "Book"; // Book, Journal, E-Book, Multimedia
    public string MetadataStandard { get; set; } = "Dublin Core";
    public bool IsDigital { get; set; }
    public string? DigitalUrl { get; set; }
    public string Barcode { get; set; } = string.Empty;
}

public class Member
{
    public int Id { get; set; }
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active";
}

public class Loan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book? Book { get; set; }
    public int MemberId { get; set; }
    public Member? Member { get; set; }
    public DateTime LoanedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueAt { get; set; } = DateTime.UtcNow.AddDays(14);
    public DateTime? ReturnedAt { get; set; }
    public decimal FineAmount { get; set; }
}

public class InventoryItem
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book? Book { get; set; }
    public int Stock { get; set; }
    public int Lost { get; set; }
    public DateTime LastAuditAt { get; set; } = DateTime.UtcNow;
}

public class Acquisition
{
    public int Id { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Cost { get; set; }
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Ordered";
}

public class AuditLog
{
    public int Id { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public int Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DiscussionTopic
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Creator { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CommunityEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow.AddDays(7);
    public string Location { get; set; } = string.Empty;
}

public class Recommendation
{
    public int Id { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}
