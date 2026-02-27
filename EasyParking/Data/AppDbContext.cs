using Microsoft.EntityFrameworkCore;
using EasyParking.Models;

namespace EasyParking.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<ParkingSlot> ParkingSlots { get; set; }
    public DbSet<ParkingTransaction> Transactions { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Additional configuration if needed
    }
}
