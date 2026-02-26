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
}

public class TaskExam
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "Quiz";
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Open";
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
    public string Score { get; set; } = string.Empty;
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
    public string FilePath { get; set; } = string.Empty;
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
