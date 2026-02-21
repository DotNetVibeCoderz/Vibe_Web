using MediaMonitoring.Models;
using MediaMonitoring.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace MediaMonitoring.Data
{
    /// <summary>
    /// Database Context untuk aplikasi Media Monitoring.
    /// Menggunakan SQLite sebagai database default.
    /// </summary>
    public class MediaMonitoringContext : DbContext
    {
        public MediaMonitoringContext(DbContextOptions<MediaMonitoringContext> options)
            : base(options)
        {
        }

        // Tabel-tabel utama
        public DbSet<MediaPost> MediaPosts { get; set; } = null!;
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; } = null!;
        public DbSet<AlertRule> AlertRules { get; set; } = null!;
        
        // Authentication & Authorization
        public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfigurasi indeks untuk performa pencarian
            modelBuilder.Entity<MediaPost>()
                .HasIndex(m => m.CreatedAt);
            
            modelBuilder.Entity<MediaPost>()
                .HasIndex(m => m.Sentiment);

            modelBuilder.Entity<MediaPost>()
                .HasIndex(m => m.Category);

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserId);
        }
    }
}