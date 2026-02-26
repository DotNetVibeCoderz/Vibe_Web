using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WMSNet.Models;

namespace WMSNet.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            // Roles
            string[] roles = { "Admin", "Mechanic", "Cashier", "Supervisor" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Admin User
            var adminEmail = "admin@wms.net";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FullName = "Super Admin",
                    Role = "Admin",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // Seed Parts if empty
            if (!context.Parts.Any())
            {
                var partFaker = new Faker<Part>()
                    .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                    .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
                    .RuleFor(p => p.SKU, f => f.Commerce.Ean13())
                    .RuleFor(p => p.PurchasePrice, f => decimal.Parse(f.Commerce.Price(10, 500)))
                    .RuleFor(p => p.SalePrice, (f, p) => p.PurchasePrice * 1.5m)
                    .RuleFor(p => p.StockQuantity, f => f.Random.Int(0, 100))
                    .RuleFor(p => p.SupplierName, f => f.Company.CompanyName());

                var parts = partFaker.Generate(50);
                await context.Parts.AddRangeAsync(parts);
                await context.SaveChangesAsync();
            }

            // Seed Customers and Vehicles
            if (!context.Customers.Any())
            {
                var vehicleFaker = new Faker<Vehicle>()
                    .RuleFor(v => v.LicensePlate, f => f.Random.Replace("B #### ???"))
                    .RuleFor(v => v.Make, f => f.Vehicle.Manufacturer())
                    .RuleFor(v => v.Model, f => f.Vehicle.Model())
                    .RuleFor(v => v.Year, f => f.Date.Past(20).Year)
                    .RuleFor(v => v.VIN, f => f.Vehicle.Vin())
                    .RuleFor(v => v.Color, f => f.Commerce.Color());

                var customerFaker = new Faker<Customer>()
                    .RuleFor(c => c.Name, f => f.Name.FullName())
                    .RuleFor(c => c.Email, f => f.Internet.Email())
                    .RuleFor(c => c.PhoneNumber, f => f.Phone.PhoneNumber("08##-####-####"))
                    .RuleFor(c => c.Address, f => f.Address.FullAddress())
                    .RuleFor(c => c.Vehicles, f => vehicleFaker.Generate(f.Random.Int(1, 3)));

                var customers = customerFaker.Generate(20);
                await context.Customers.AddRangeAsync(customers);
                await context.SaveChangesAsync();
            }

            // Seed Jobs
            if (!context.ServiceJobs.Any())
            {
                var customers = await context.Customers.Include(c => c.Vehicles).ToListAsync();
                var parts = await context.Parts.ToListAsync();
                var allVehicles = customers.SelectMany(c => c.Vehicles).ToList();

                var jobFaker = new Faker<ServiceJob>()
                    .RuleFor(j => j.Description, f => f.Lorem.Sentence())
                    .RuleFor(j => j.MechanicNotes, f => f.Lorem.Paragraph())
                    .RuleFor(j => j.Status, f => f.PickRandom<JobStatus>())
                    .RuleFor(j => j.CreatedAt, f => f.Date.Past(1))
                    .RuleFor(j => j.VehicleId, f => f.PickRandom(allVehicles).Id);

                var jobs = jobFaker.Generate(30);

                foreach (var job in jobs)
                {
                    // Add items
                    var numItems = new Random().Next(1, 5);
                    for (int i = 0; i < numItems; i++)
                    {
                        var part = parts[new Random().Next(parts.Count)];
                        job.Items.Add(new JobItem
                        {
                            PartId = part.Id,
                            ServiceName = part.Name,
                            Quantity = new Random().Next(1, 3),
                            UnitPrice = part.SalePrice
                        });
                    }
                    
                    // Add some pure service items (Labor)
                    job.Items.Add(new JobItem
                    {
                        ServiceName = "Jasa Mekanik",
                        Quantity = 1,
                        UnitPrice = new Random().Next(50000, 200000)
                    });
                }

                await context.ServiceJobs.AddRangeAsync(jobs);
                await context.SaveChangesAsync();
            }
        }
    }
}