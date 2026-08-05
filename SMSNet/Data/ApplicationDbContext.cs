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

    // QR attendance — the editable card layout.
    public DbSet<CardTemplate> CardTemplates => Set<CardTemplate>();

    // Payments — gateway settings and the transaction ledger.
    public DbSet<PaymentGatewayConfig> PaymentGatewayConfigs => Set<PaymentGatewayConfig>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    // Assistant ("Pak Dedi") conversation storage.
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatAttachment> ChatAttachments => Set<ChatAttachment>();

    // Discussion threads attached to forum topics and KPI indicators.
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentAttachment> CommentAttachments => Set<CommentAttachment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // The chat tables are the one place in this schema with real relationships,
        // so deleting a thread has to take its messages and files with it.
        builder.Entity<ChatSession>(entity =>
        {
            entity.HasIndex(s => new { s.UserId, s.UpdatedAt });

            entity.HasMany(s => s.Messages)
                .WithOne(m => m.ChatSession!)
                .HasForeignKey(m => m.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Comment>(entity =>
        {
            // Every read is "the comments on this one record", so the composite index
            // matches the only access pattern there is.
            entity.HasIndex(c => new { c.ThreadType, c.ThreadId });

            entity.HasMany(c => c.Attachments)
                .WithOne(a => a.Comment!)
                .HasForeignKey(a => a.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // A scanned code must resolve to exactly one person, so the index is unique.
        // Filtered so the many rows still awaiting a code don't collide on NULL.
        builder.Entity<Student>()
            .HasIndex(s => s.QrCode)
            .IsUnique()
            .HasFilter("\"QrCode\" IS NOT NULL");

        builder.Entity<Teacher>()
            .HasIndex(t => t.QrCode)
            .IsUnique()
            .HasFilter("\"QrCode\" IS NOT NULL");

        // One active layout per card kind.
        builder.Entity<CardTemplate>()
            .HasIndex(c => c.Kind)
            .IsUnique();

        // One settings row per provider, so an override can never be ambiguous.
        builder.Entity<PaymentGatewayConfig>(entity =>
        {
            entity.HasIndex(c => c.Key).IsUnique();
        });

        builder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasIndex(t => t.Reference).IsUnique();
            entity.HasIndex(t => t.Status);
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.Fee).HasPrecision(18, 2);
        });

        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasIndex(m => m.ChatSessionId);

            entity.HasMany(m => m.Attachments)
                .WithOne(a => a.ChatMessage!)
                .HasForeignKey(a => a.ChatMessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
