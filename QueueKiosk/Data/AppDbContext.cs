using Microsoft.EntityFrameworkCore;
using QueueKiosk.Models;

namespace QueueKiosk.Data;

public class AppDbContext : DbContext
{
    public DbSet<AppUser> Users { get; set; } = null!;
    public DbSet<AppService> Services { get; set; } = null!;
    public DbSet<AppCounter> Counters { get; set; } = null!;
    public DbSet<AppQueueTicket> QueueTickets { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed default Admin
        modelBuilder.Entity<AppUser>().HasData(
            new AppUser { Id = 1, Username = "admin", PasswordHash = "admin123", Role = "admin" },
            new AppUser { Id = 2, Username = "user", PasswordHash = "user123", Role = "user" }
        );

        // Seed Services
        modelBuilder.Entity<AppService>().HasData(
            new AppService { Id = 1, Name = "Customer Service", LetterCode = "CS", Description = "General Inquiries" },
            new AppService { Id = 2, Name = "Teller", LetterCode = "T", Description = "Cash Transactions" },
            new AppService { Id = 3, Name = "VIP Services", LetterCode = "V", Description = "Priority Clients" }
        );

        // Seed Counters
        modelBuilder.Entity<AppCounter>().HasData(
            new AppCounter { Id = 1, Name = "Counter 1" },
            new AppCounter { Id = 2, Name = "Counter 2" },
            new AppCounter { Id = 3, Name = "Counter 3" }
        );

        // Optional: Seed QueueTickets? Usually dynamically generated, but we can seed some historical for analytics
        var random = new Random(1);
        var baseDate = DateTime.Today.AddDays(-3);
        var tickets = new List<AppQueueTicket>();
        int idCounter = 1;
        for (int i = 0; i < 50; i++)
        {
            var serviceId = random.Next(1, 4);
            var isPriority = random.Next(0, 5) == 0;
            var created = baseDate.AddMinutes(random.Next(0, 1440));
            tickets.Add(new AppQueueTicket
            {
                Id = idCounter++,
                ServiceId = serviceId,
                TicketNumber = $"{(serviceId == 1 ? "CS" : (serviceId == 2 ? "T" : "V"))}-{i + 1}",
                IsPriority = isPriority,
                CreatedAt = created,
                CalledAt = created.AddMinutes(random.Next(5, 30)),
                CompletedAt = created.AddMinutes(random.Next(10, 60)),
                Status = "Completed",
                CounterId = random.Next(1, 4)
            });
        }
        modelBuilder.Entity<AppQueueTicket>().HasData(tickets);
    }
}
