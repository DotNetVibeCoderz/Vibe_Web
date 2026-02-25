using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyAsset.Models;

namespace MyAsset.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Asset> Assets { get; set; } = default!;
        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<MaintenanceLog> MaintenanceLogs { get; set; } = default!;
    }
}