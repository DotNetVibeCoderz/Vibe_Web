using Microsoft.EntityFrameworkCore;
using FormDezigner.Models;

namespace FormDezigner.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FormEntity> Forms { get; set; }
        public DbSet<FormVersion> FormVersions { get; set; }
    }
}
