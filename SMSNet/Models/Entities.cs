using System.ComponentModel.DataAnnotations;

namespace SMSNet.Models;

public class Student
{
    public int Id { get; set; }
    [Required]
    public string FullName { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? ParentName { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "Active";

    /// <summary>
    /// The value printed on the student's card and read by the attendance scanner.
    /// Stored rather than derived so a lost card can be reissued with a new code
    /// while the old one stops working. Assigned by <c>QrCodeService</c>.
    /// </summary>
    [MaxLength(40)]
    public string? QrCode { get; set; }
}

public class Teacher
{
    public int Id { get; set; }
    [Required]
    public string FullName { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "Active";

    /// <summary>See <see cref="Student.QrCode"/>.</summary>
    [MaxLength(40)]
    public string? QrCode { get; set; }
}

/// <summary>
/// The printable card layout, stored as editable HTML.
/// <para>
/// A default template ships as a file under <c>wwwroot/templates/</c>; saving from
/// the admin screen writes a row here, which then wins. That way a fresh install
/// works with no database content, and a school can restyle its cards without
/// touching the filesystem.
/// </para>
/// </summary>
public class CardTemplate
{
    public int Id { get; set; }

    /// <summary>Which card this lays out: <c>siswa</c> or <c>guru</c>.</summary>
    [Required, MaxLength(20)]
    public string Kind { get; set; } = "siswa";

    [Required, MaxLength(120)]
    public string Name { get; set; } = "Kartu Siswa";

    /// <summary>HTML with <c>{{PLACEHOLDER}}</c> tokens. See <c>CardTemplateService</c>.</summary>
    public string Html { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class ParentGuardian
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? StudentName { get; set; }
}

public class ClassRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? HomeroomTeacher { get; set; }
    public int Capacity { get; set; }
}

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string? Description { get; set; }
}

public class CurriculumItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GradeLevel { get; set; }
    public string? Description { get; set; }
}

public class ScheduleItem
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
}

public class AttendanceRecord
{
    public int Id { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Present";
    public string Method { get; set; } = "Barcode";
}

public class GradeRecord
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// The student's class, captured when the grade is entered.
    /// <para>
    /// Denormalised on purpose: a grade is a record of a moment. Reading the class
    /// from the student row instead would silently rewrite last year's results when
    /// a student is promoted.
    /// </para>
    /// </summary>
    public string? ClassName { get; set; }

    public string Subject { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string? Notes { get; set; }
}

public class ELearningContent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ModuleType { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Where the material actually lives — a video, a document, a quiz form.</summary>
    public string? LinkUrl { get; set; }
}

public class TaskExam
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "Quiz";
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Open";

    /// <summary>Optional link to the assignment or exam itself.</summary>
    public string? LinkUrl { get; set; }

    /// <summary>
    /// Classes this applies to, comma-separated; empty means every class.
    /// <para>
    /// A joining table would be the normalised answer, but every relationship in
    /// this schema is a display string and the seeder and all reporting pages join
    /// by name — one real FK here would be the odd one out. See CLAUDE.md.
    /// </para>
    /// </summary>
    public string? Classes { get; set; }
}

public class ForumPost
{
    public int Id { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime PostedAt { get; set; }
}

public class PerformanceReview
{
    public int Id { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string KPI { get; set; } = string.Empty;

    /// <summary>The achievement as typed — "95%", "4.2", or a letter grade.</summary>
    public string Score { get; set; } = string.Empty;

    /// <summary>
    /// How <see cref="Score"/> should be read: <c>Persen</c>, <c>Skala</c> (0–5),
    /// or <c>Teks</c>.
    /// <para>
    /// Without this the progress meter has to guess, and it guessed wrong: "4.2"
    /// on a five-point scale was drawn as 4% rather than 84%. Storing the unit is
    /// also what makes validation deterministic instead of heuristic.
    /// </para>
    /// </summary>
    public string Unit { get; set; } = "Persen";
}

public class PaymentRecord
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Category { get; set; } = "SPP";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Paid";
    public DateTime Date { get; set; }
}

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Condition { get; set; } = "Good";
}

public class PayrollRecord
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public DateTime Period { get; set; }
}

public class FinancialReport
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public DateTime Period { get; set; }
}

public class NotificationItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Audience { get; set; } = string.Empty;
}

public class DocumentItem
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>URL of the document — an uploaded file under wwwroot, or an
    /// external link.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>The name the file was uploaded under, for display. The stored path
    /// is generated, so without this the list can only show an opaque filename.</summary>
    public string? FileName { get; set; }

    public long? SizeBytes { get; set; }

    /// <summary>True when this app holds the file, false when it only links out.
    /// Deleting the record removes the file only in the first case.</summary>
    public bool IsUploaded { get; set; }

    public DateTime? UploadedAt { get; set; }
}

public class AuditTrail
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Detail { get; set; }
}

public class EventItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
