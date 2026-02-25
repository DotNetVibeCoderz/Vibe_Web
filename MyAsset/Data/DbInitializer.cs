using Microsoft.AspNetCore.Identity;
using MyAsset.Models;

namespace MyAsset.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Seed Roles
            string[] roleNames = { "Admin", "Manager", "Operator" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin User
            if (userManager.Users.All(u => u.UserName != "admin"))
            {
                var user = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@myasset.com",
                    FullName = "Super Admin",
                    Department = "IT",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }

            // Seed Categories
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Hardware", Description = "Physical IT equipment like Laptops, Servers" },
                    new Category { Name = "Software", Description = "Licenses and Subscriptions" },
                    new Category { Name = "Peripherals", Description = "Keyboards, Mice, Monitors" },
                    new Category { Name = "Furniture", Description = "Chairs, Desks" }
                );
                await context.SaveChangesAsync();
            }

            // Seed Assets (IT Department Focus)
            if (!context.Assets.Any())
            {
                var hwCat = context.Categories.First(c => c.Name == "Hardware");
                var swCat = context.Categories.First(c => c.Name == "Software");
                var furnCat = context.Categories.First(c => c.Name == "Furniture");

                // Using Jakarta coordinates as example base
                // Jakarta: -6.2088, 106.8456

                var assets = new List<Asset>
                {
                    new Asset { Name = "Dell XPS 15", SerialNumber = "DXPS15-001", Category = hwCat, Location = "Head Office - Server Room", Latitude = -6.2088, Longitude = 106.8456, PurchasePrice = 2500, PurchaseDate = DateTime.Now.AddMonths(-12), Status = AssetStatus.InUse, AssignedTo = "John Doe", ExpectedLifeSpanMonths = 48 },
                    new Asset { Name = "MacBook Pro M2", SerialNumber = "MBP-M2-002", Category = hwCat, Location = "Design Studio Branch", Latitude = -6.2250, Longitude = 106.8000, PurchasePrice = 3000, PurchaseDate = DateTime.Now.AddMonths(-2), Status = AssetStatus.InUse, AssignedTo = "Jane Smith", ExpectedLifeSpanMonths = 48 },
                    new Asset { Name = "HP ProLiant Server", SerialNumber = "HPSRV-003", Category = hwCat, Location = "Data Center Bekasi", Latitude = -6.2415, Longitude = 106.9924, PurchasePrice = 8000, PurchaseDate = DateTime.Now.AddMonths(-24), Status = AssetStatus.InUse, AssignedTo = "IT Admin", ExpectedLifeSpanMonths = 60 },
                    new Asset { Name = "Windows Server 2022 License", SerialNumber = "WINSRV-LIC-001", Category = swCat, Location = "Digital - Cloud", Latitude = 0, Longitude = 0, PurchasePrice = 1200, PurchaseDate = DateTime.Now.AddMonths(-6), Status = AssetStatus.InUse, AssignedTo = "System", ExpectedLifeSpanMonths = 36 },
                    new Asset { Name = "Visual Studio Enterprise", SerialNumber = "VSENT-LIC-002", Category = swCat, Location = "Digital - Dev", Latitude = 0, Longitude = 0, PurchasePrice = 2500, PurchaseDate = DateTime.Now.AddMonths(-1), Status = AssetStatus.InUse, AssignedTo = "Dev Team", ExpectedLifeSpanMonths = 12 },
                    new Asset { Name = "Cisco Switch 2960", SerialNumber = "CSCO-SW-004", Category = hwCat, Location = "Warehouse Logic", Latitude = -6.1554, Longitude = 106.8926, PurchasePrice = 500, PurchaseDate = DateTime.Now.AddMonths(-30), Status = AssetStatus.UnderMaintenance, AssignedTo = "Network Eng", ExpectedLifeSpanMonths = 60 },
                    new Asset { Name = "Lenovo ThinkPad X1", SerialNumber = "LNV-TP-005", Category = hwCat, Location = "Sales Office South", Latitude = -6.2800, Longitude = 106.7800, PurchasePrice = 1800, PurchaseDate = DateTime.Now.AddMonths(-10), Status = AssetStatus.InUse, AssignedTo = "Sales Manager", ExpectedLifeSpanMonths = 36 },
                    new Asset { Name = "Office Chair Ergo", SerialNumber = "FURN-CH-001", Category = furnCat, Location = "Head Office - Room 101", Latitude = -6.2090, Longitude = 106.8460, PurchasePrice = 300, PurchaseDate = DateTime.Now.AddMonths(-5), Status = AssetStatus.InUse, AssignedTo = "Staff", ExpectedLifeSpanMonths = 60 }
                };

                context.Assets.AddRange(assets);
                await context.SaveChangesAsync();

                // Add some maintenance history
                var srv = context.Assets.First(a => a.Name == "HP ProLiant Server");
                context.MaintenanceLogs.Add(new MaintenanceLog
                {
                    AssetId = srv.Id,
                    Description = "Routine Fan Cleaning",
                    Date = DateTime.Now.AddMonths(-12),
                    Cost = 150,
                    TechnicianName = "Ext Vendor A"
                });
                context.MaintenanceLogs.Add(new MaintenanceLog
                {
                    AssetId = srv.Id,
                    Description = "HDD Replacement (RAID 5 failure)",
                    Date = DateTime.Now.AddMonths(-2),
                    Cost = 400,
                    TechnicianName = "Internal IT"
                });
                
                srv.MaintenanceCount = 2;
                context.Update(srv);
                await context.SaveChangesAsync();
            }
        }
    }
}