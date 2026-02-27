using AISTracker.Data;
using AISTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AISTracker.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Pastikan database dan tabel Identity tersedia
                await EnsureDatabaseCreatedAsync(context);

                // Roles
                string[] roles = { "Admin", "Operator", "Manager" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                // Admin User
                var adminEmail = "admin@aistracker.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin",
                        Email = adminEmail,
                        FullName = "Administrator",
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(adminUser, "Admin123!");
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                // Sample Vessels
                if (!context.Vessels.Any())
                {
                    context.Vessels.AddRange(
                        new Vessel { MMSI = "123456789", Name = "Ocean Explorer", IMONumber = "IMO1234567", Flag = "USA", VesselType = "Cargo", Length = 200, Width = 30, Status = "Active" },
                        new Vessel { MMSI = "987654321", Name = "Sea Guardian", IMONumber = "IMO2345678", Flag = "UK", VesselType = "Tanker", Length = 250, Width = 40, Status = "Active" },
                        new Vessel { MMSI = "112233445", Name = "Pacific Star", IMONumber = "IMO3456789", Flag = "Panama", VesselType = "Passenger", Length = 150, Width = 25, Status = "Maintenance" },
                        new Vessel { MMSI = "556677889", Name = "Arctic Wind", IMONumber = "IMO4567890", Flag = "Norway", VesselType = "Fishing", Length = 50, Width = 10, Status = "Active" },
                        new Vessel { MMSI = "998877665", Name = "Blue Horizon", IMONumber = "IMO5678901", Flag = "Singapore", VesselType = "Container", Length = 300, Width = 45, Status = "Active" }
                    );
                    await context.SaveChangesAsync();
                }

                // Sample Ports
                if (!context.Ports.Any())
                {
                    context.Ports.AddRange(
                        new Port { Name = "Port of Singapore", Country = "Singapore", Latitude = 1.264, Longitude = 103.840, UNLocCode = "SGSIN" },
                        new Port { Name = "Port of Rotterdam", Country = "Netherlands", Latitude = 51.950, Longitude = 4.150, UNLocCode = "NLRTM" },
                        new Port { Name = "Port of Shanghai", Country = "China", Latitude = 31.230, Longitude = 121.470, UNLocCode = "CNSHA" }
                    );
                    await context.SaveChangesAsync();
                }
            }
        }

        private static async Task EnsureDatabaseCreatedAsync(ApplicationDbContext context)
        {
            var database = context.Database;

            // Jika migration tersedia, jalankan migrate. Jika tidak, gunakan EnsureCreated.
            if (database.GetMigrations().Any())
            {
                await database.MigrateAsync();
            }
            else
            {
                await database.EnsureCreatedAsync();
            }

            // Cek keberadaan tabel Identity. Jika tidak ada, reset database agar schema lengkap terbentuk.
            if (!await IdentityTablesExistAsync(context))
            {
                await database.EnsureDeletedAsync();
                await database.EnsureCreatedAsync();
            }
        }

        private static async Task<bool> IdentityTablesExistAsync(ApplicationDbContext context)
        {
            await using var connection = context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AspNetRoles';";
            var result = await command.ExecuteScalarAsync();

            return result != null && Convert.ToInt32(result) > 0;
        }
    }
}
