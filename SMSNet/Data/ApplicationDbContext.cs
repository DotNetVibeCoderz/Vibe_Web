using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SMSNet.Models;

namespace SMSNet.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<ParentGuardian> Parents => Set<ParentGuardian>();
    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<CurriculumItem> CurriculumItems => Set<CurriculumItem>();
    public DbSet<ScheduleItem> ScheduleItems => Set<ScheduleItem>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<GradeRecord> GradeRecords => Set<GradeRecord>();
    public DbSet<ELearningContent> ELearningContents => Set<ELearningContent>();
    public DbSet<TaskExam> TaskExams => Set<TaskExam>();
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<FinancialReport> FinancialReports => Set<FinancialReport>();
    public DbSet<NotificationItem> Notifications => Set<NotificationItem>();
    public DbSet<DocumentItem> Documents => Set<DocumentItem>();
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
    public DbSet<EventItem> Events => Set<EventItem>();
}
