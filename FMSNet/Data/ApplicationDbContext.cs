using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FMSNet.Data;

namespace FMSNet.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<FuelLog> FuelLogs { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize your model logic here
            builder.Entity<Vehicle>().HasData(
                new Vehicle { Id = 1, Name = "Truck-01", Type = "Truck", LicensePlate = "B 1234 ABC", Status = "Active", Latitude = -6.2088, Longitude = 106.8456, FuelLevel = 80, Odometer = 12000, LastUpdate = DateTime.UtcNow },
                new Vehicle { Id = 2, Name = "Van-02", Type = "Van", LicensePlate = "D 5678 EFG", Status = "Maintenance", Latitude = -6.9175, Longitude = 107.6191, FuelLevel = 45, Odometer = 8000, LastUpdate = DateTime.UtcNow },
                new Vehicle { Id = 3, Name = "Car-03", Type = "Car", LicensePlate = "L 9012 HIJ", Status = "Active", Latitude = -7.2575, Longitude = 112.7521, FuelLevel = 90, Odometer = 5000, LastUpdate = DateTime.UtcNow }
            );

            builder.Entity<Driver>().HasData(
                new Driver { Id = 1, Name = "John Doe", LicenseNumber = "DL-001", Status = "Available", SafetyScore = 95 },
                new Driver { Id = 2, Name = "Jane Smith", LicenseNumber = "DL-002", Status = "OnDuty", SafetyScore = 98 },
                new Driver { Id = 3, Name = "Mike Johnson", LicenseNumber = "DL-003", Status = "OffDuty", SafetyScore = 88 }
            );

            builder.Entity<ApiKey>().HasData(
                new ApiKey { Id = 1, Key = "FMS-API-KEY-12345", Description = "Default API Key", IsActive = true, CreatedAt = DateTime.UtcNow }
            );
        }
    }
}