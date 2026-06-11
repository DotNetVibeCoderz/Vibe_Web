using Microsoft.EntityFrameworkCore;
using VirtualDoctor.Models;
using VirtualDoctor.Services;

namespace VirtualDoctor.Data;

/// <summary>
/// Main database context untuk VirtualDoctor
/// Mendukung multiple provider: SQLite, SQL Server, MySQL, PostgreSQL
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Core entities
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Hospital> Hospitals => Set<Hospital>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<HealthArticle> HealthArticles => Set<HealthArticle>();
    public DbSet<PasswordHash> PasswordHashes => Set<PasswordHash>();
    public DbSet<DoctorReview> DoctorReviews => Set<DoctorReview>();

    // Transactional entities
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<ConsultationMessage> ConsultationMessages => Set<ConsultationMessage>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<HomecareService> HomecareServices => Set<HomecareService>();

    // AI Chat
    public DbSet<ChatHistory> ChatHistories => Set<ChatHistory>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PasswordHash
        modelBuilder.Entity<PasswordHash>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.UserId).IsUnique();
        });

        // User configuration
        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
        });

        // Doctor configuration
        modelBuilder.Entity<Doctor>(e => e.HasKey(d => d.Id));

        // Doctor reviews
        modelBuilder.Entity<DoctorReview>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.DoctorId, r.UserId });
            e.HasOne(r => r.Doctor).WithMany(d => d.Reviews).HasForeignKey(r => r.DoctorId);
            e.HasOne(r => r.User).WithMany(u => u.DoctorReviews).HasForeignKey(r => r.UserId);
            e.HasOne(r => r.Consultation).WithMany().HasForeignKey(r => r.ConsultationId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.Appointment).WithMany().HasForeignKey(r => r.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        });

        // Consultation relationships
        modelBuilder.Entity<Consultation>(e =>
        {
            e.HasOne(c => c.User).WithMany(u => u.Consultations).HasForeignKey(c => c.UserId);
            e.HasOne(c => c.Doctor).WithMany(d => d.Consultations).HasForeignKey(c => c.DoctorId);
        });

        modelBuilder.Entity<ConsultationMessage>(e =>
            e.HasOne(m => m.Consultation).WithMany(c => c.Messages).HasForeignKey(m => m.ConsultationId));

        // Appointment relationships
        modelBuilder.Entity<Appointment>(e =>
        {
            e.HasOne(a => a.User).WithMany(u => u.Appointments).HasForeignKey(a => a.UserId);
            e.HasOne(a => a.Doctor).WithMany(d => d.Appointments).HasForeignKey(a => a.DoctorId);
            e.HasOne(a => a.Hospital).WithMany(h => h.Appointments).HasForeignKey(a => a.HospitalId);
        });

        // Order relationships
        modelBuilder.Entity<Order>(e =>
        {
            e.HasOne(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId);
            e.HasOne(o => o.Pharmacy).WithMany().HasForeignKey(o => o.PharmacyId);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasOne(oi => oi.Order).WithMany(o => o.Items).HasForeignKey(oi => oi.OrderId);
            e.HasOne(oi => oi.Medicine).WithMany().HasForeignKey(oi => oi.MedicineId);
        });

        // DoctorSchedule
        modelBuilder.Entity<DoctorSchedule>(e =>
            e.HasOne(s => s.Doctor).WithMany(d => d.Schedules).HasForeignKey(s => s.DoctorId));

        // HomecareService
        modelBuilder.Entity<HomecareService>(e =>
            e.HasOne(h => h.User).WithMany().HasForeignKey(h => h.UserId));

        // ChatHistory
        modelBuilder.Entity<ChatHistory>(e =>
            e.HasOne(ch => ch.User).WithMany(u => u.ChatHistories).HasForeignKey(ch => ch.UserId));

        modelBuilder.Entity<ChatMessage>(e =>
            e.HasOne(cm => cm.ChatHistory).WithMany(ch => ch.Messages).HasForeignKey(cm => cm.ChatHistoryId));

        // Hospital indexes
        modelBuilder.Entity<Hospital>(e =>
        {
            e.HasKey(h => h.Id);
            e.HasIndex(h => h.City);
            e.HasIndex(h => h.Type);
        });
    }
}
