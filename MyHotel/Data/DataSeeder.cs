using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using MyHotel.Models;

namespace MyHotel.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            context.Database.EnsureCreated();

            // Seed Roles
            string[] roles = { "Admin", "Manager", "Operator" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                adminUser = new IdentityUser { UserName = "admin", Email = "admin@myhotel.com", EmailConfirmed = true };
                var result = await userManager.CreateAsync(adminUser, "admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Rooms
            if (!context.Rooms.Any())
            {
                for (int i = 1; i <= 50; i++)
                {
                    string type = i <= 20 ? "Standard" : (i <= 40 ? "Deluxe" : "Suite");
                    decimal price = type == "Standard" ? 500000 : (type == "Deluxe" ? 1000000 : 2500000);
                    string status = "Available";
                    if (i % 5 == 0) status = "Cleaning";
                    if (i % 7 == 0) status = "Occupied";

                    context.Rooms.Add(new Room
                    {
                        Number = (100 + i).ToString(),
                        Type = type,
                        Status = status,
                        Price = price,
                        Notes = $"Beautiful {type} room"
                    });
                }
                context.SaveChanges();
            }

            // Seed Guests
            if (!context.Guests.Any())
            {
                for (int i = 1; i <= 30; i++)
                {
                    context.Guests.Add(new Guest
                    {
                        FirstName = "Guest",
                        LastName = i.ToString(),
                        Email = $"guest{i}@example.com",
                        Phone = $"0812345678{i:D2}",
                        Address = $"Jl. Contoh No. {i}",
                        Preference = i % 2 == 0 ? "Non-smoking" : "Smoking"
                    });
                }
                context.SaveChanges();
            }

            // Seed Reservations
            if (!context.Reservations.Any())
            {
                var rooms = context.Rooms.ToList();
                var guests = context.Guests.ToList();
                var random = new Random();

                for (int i = 0; i < 20; i++)
                {
                    var guest = guests[random.Next(guests.Count)];
                    var room = rooms[random.Next(rooms.Count)];
                    var checkIn = DateTime.Now.AddDays(random.Next(-5, 5));
                    var checkOut = checkIn.AddDays(random.Next(1, 5));
                    
                    context.Reservations.Add(new Reservation
                    {
                        GuestId = guest.Id,
                        RoomId = room.Id,
                        CheckInDate = checkIn,
                        CheckOutDate = checkOut,
                        Status = "Confirmed",
                        TotalAmount = room.Price * (checkOut - checkIn).Days
                    });
                }
                context.SaveChanges();
            }
        }
    }
}