using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AISTracker.Models;

namespace AISTracker.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vessel> Vessels { get; set; }
        public DbSet<PositionReport> PositionReports { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<Port> Ports { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Configure relationships if needed
            builder.Entity<PositionReport>()
                .HasOne(p => p.Vessel)
                .WithMany(v => v.PositionReports)
                .HasForeignKey(p => p.VesselId);
        }
    }
}
