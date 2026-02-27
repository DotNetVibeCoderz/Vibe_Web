using EasyParking.Models;

namespace EasyParking.Data;

public static class DatabaseSeeder
{
    public static void Seed(AppDbContext context)
    {
        context.Database.EnsureCreated();

        // Seed Users
        if (!context.Users.Any())
        {
            context.Users.AddRange(
                new User { Username = "admin", Password = "admin123", Role = "admin", Email = "admin@easyparking.com" },
                new User { Username = "operator1", Password = "password123", Role = "operator", Email = "ops1@easyparking.com" },
                new User { Username = "user1", Password = "password123", Role = "pelanggan", Email = "user1@easyparking.com" }
            );
            context.SaveChanges();
        }

        // Seed Slots
        if (!context.ParkingSlots.Any())
        {
            var slots = new List<ParkingSlot>();
            string[] levels = { "L1", "L2", "L3" };
            
            foreach (var level in levels)
            {
                for (int i = 1; i <= 20; i++)
                {
                    slots.Add(new ParkingSlot
                    {
                        Code = $"{level}-{i:D2}",
                        Level = level,
                        IsOccupied = i % 5 == 0, // Mock 20% occupied
                        IsEV = (level == "L1" && i <= 5), // First 5 on L1 are EV
                        IsReserved = i % 7 == 0,
                        Type = i > 15 ? "Bike" : "Car"
                    });
                }
            }
            context.ParkingSlots.AddRange(slots);
            context.SaveChanges();
        }

        // Seed Transactions
        if (!context.Transactions.Any())
        {
            var occupiedSlots = context.ParkingSlots.Where(s => s.IsOccupied).ToList();
            var rand = new Random();
            foreach (var slot in occupiedSlots)
            {
                context.Transactions.Add(new ParkingTransaction
                {
                    VehiclePlate = $"B {rand.Next(1000, 9999)} ABC",
                    ParkingSlotId = slot.Id,
                    CheckInTime = DateTime.Now.AddHours(-rand.Next(1, 10)),
                    Status = "Active"
                });
            }
            context.SaveChanges();
        }
    }
}
