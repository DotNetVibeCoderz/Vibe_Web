using Microsoft.AspNetCore.Identity;
using MyResto.Models;

namespace MyResto.Data
{
    public static class DataSeeder
    {
        public static async Task SeedData(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            string[] roles = { "admin", "kasir", "waiter", "manager", "chef" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Default Admin User
            var adminEmail = "admin@myresto.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FullName = "Super Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "admin");
                }
            }

            // Seed Master Data
            if (!dbContext.Categories.Any())
            {
                var catMakanan = new Category { Name = "Makanan Utama", Description = "Hidangan Berat" };
                var catMinuman = new Category { Name = "Minuman", Description = "Minuman Segar" };
                var catDessert = new Category { Name = "Dessert", Description = "Makanan Penutup" };

                dbContext.Categories.AddRange(catMakanan, catMinuman, catDessert);
                await dbContext.SaveChangesAsync();

                dbContext.Products.AddRange(
                    new Product { Name = "Nasi Goreng Spesial", Description = "Nasi goreng dengan telur, ayam, dan sosis", Price = 35000, CategoryId = catMakanan.Id, StockQuantity = 50 },
                    new Product { Name = "Mie Goreng Jawa", Description = "Mie goreng bumbu tradisional", Price = 30000, CategoryId = catMakanan.Id, StockQuantity = 40 },
                    new Product { Name = "Es Teh Manis", Description = "Teh manis dingin segar", Price = 10000, CategoryId = catMinuman.Id, StockQuantity = 100 },
                    new Product { Name = "Jus Jeruk", Description = "Jus jeruk peras murni", Price = 15000, CategoryId = catMinuman.Id, StockQuantity = 50 },
                    new Product { Name = "Pudding Coklat", Description = "Pudding coklat lumer", Price = 20000, CategoryId = catDessert.Id, StockQuantity = 30 }
                );
                await dbContext.SaveChangesAsync();
            }

            if (!dbContext.Tables.Any())
            {
                dbContext.Tables.AddRange(
                    new RestaurantTable { Name = "Table 01", Capacity = 4 },
                    new RestaurantTable { Name = "Table 02", Capacity = 4 },
                    new RestaurantTable { Name = "Table 03", Capacity = 2 },
                    new RestaurantTable { Name = "Table 04", Capacity = 6 },
                    new RestaurantTable { Name = "VIP 1", Capacity = 8 }
                );
                await dbContext.SaveChangesAsync();
            }
            
            if (!dbContext.Ingredients.Any())
            {
                dbContext.Ingredients.AddRange(
                    new Ingredient { Name = "Beras", Unit = "kg", CurrentStock = 50, MinimumStock = 10 },
                    new Ingredient { Name = "Minyak Goreng", Unit = "L", CurrentStock = 20, MinimumStock = 5 },
                    new Ingredient { Name = "Daging Ayam", Unit = "kg", CurrentStock = 15, MinimumStock = 5 },
                    new Ingredient { Name = "Gula Pasir", Unit = "kg", CurrentStock = 10, MinimumStock = 2 },
                    new Ingredient { Name = "Kopi Biji", Unit = "kg", CurrentStock = 5, MinimumStock = 1 }
                );
                await dbContext.SaveChangesAsync();
            }

            if (!dbContext.Employees.Any())
            {
                dbContext.Employees.AddRange(
                    new Employee { Name = "Budi Santoso", Role = "Manager", PhoneNumber = "081234567890", Email = "budi@myresto.com" },
                    new Employee { Name = "Siti Aminah", Role = "Chef", PhoneNumber = "081298765432", Email = "siti@myresto.com" },
                    new Employee { Name = "Agus Pratama", Role = "Waiter", PhoneNumber = "081345678901", Email = "agus@myresto.com" },
                    new Employee { Name = "Dewi Lestari", Role = "Cashier", PhoneNumber = "081456789012", Email = "dewi@myresto.com" }
                );
                await dbContext.SaveChangesAsync();
            }

            if (!dbContext.Customers.Any())
            {
                dbContext.Customers.AddRange(
                    new Customer { Name = "Andi Wijaya", PhoneNumber = "08111111111", Email = "andi@gmail.com", LoyaltyPoints = 150, LastVisit = DateTime.UtcNow.AddDays(-2) },
                    new Customer { Name = "Rina Gunawan", PhoneNumber = "08222222222", Email = "rina@yahoo.com", LoyaltyPoints = 50, LastVisit = DateTime.UtcNow.AddDays(-10) },
                    new Customer { Name = "Doni Kusuma", PhoneNumber = "08333333333", Email = "doni@hotmail.com", LoyaltyPoints = 300, LastVisit = DateTime.UtcNow.AddHours(-5) }
                );
                await dbContext.SaveChangesAsync();
            }
        }
    }
}