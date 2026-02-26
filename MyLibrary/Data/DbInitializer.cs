using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyLibrary.Models;

namespace MyLibrary.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        try
        {
            await SeedWithScopeAsync(services);
        }
        catch (DbUpdateException)
        {
            // Jika schema DB lama tidak cocok (FK gagal), reset DB untuk memastikan seeding konsisten.
            await ResetDatabaseAsync(services);
            await SeedWithScopeAsync(services);
        }
    }

    private static async Task SeedWithScopeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await SeedAsync(context, userManager, roleManager);
    }

    private static async Task ResetDatabaseAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureDeletedAsync();
    }

    private static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await context.Database.EnsureCreatedAsync();

        var roles = new[] { "admin", "petugas", "anggota" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@library.local",
                FullName = "Administrator",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, "admin123");
            await userManager.AddToRoleAsync(adminUser, "admin");
        }

        if (!context.Books.Any())
        {
            var books = new List<Book>();
            for (var i = 1; i <= 25; i++)
            {
                books.Add(new Book
                {
                    Title = $"Sample Book {i}",
                    Author = i % 2 == 0 ? "Dewi Lestari" : "Tere Liye",
                    ISBN = $"978-0000{i:000}",
                    Category = i % 3 == 0 ? "Technology" : "Literature",
                    Type = i % 5 == 0 ? "E-Book" : "Book",
                    MetadataStandard = i % 2 == 0 ? "MARC" : "Dublin Core",
                    IsDigital = i % 5 == 0,
                    DigitalUrl = i % 5 == 0 ? "https://example.com/ebooks" : null,
                    Barcode = $"LIB-{1000 + i}"
                });
            }

            context.Books.AddRange(books);
        }

        if (!context.Members.Any())
        {
            var members = new List<Member>();
            for (var i = 1; i <= 12; i++)
            {
                members.Add(new Member
                {
                    Name = $"Member {i}",
                    Email = $"member{i}@mail.com",
                    Phone = $"08123{i:0000}",
                    JoinedAt = DateTime.UtcNow.AddDays(-i * 10),
                    Status = i % 4 == 0 ? "Suspended" : "Active"
                });
            }
            context.Members.AddRange(members);
        }

        // Simpan lebih dulu agar ID tersedia untuk relasi berikutnya.
        await context.SaveChangesAsync();

        var firstBook = await context.Books.OrderBy(b => b.Id).FirstOrDefaultAsync();
        var secondBook = await context.Books.OrderBy(b => b.Id).Skip(1).FirstOrDefaultAsync();
        var firstMember = await context.Members.OrderBy(m => m.Id).FirstOrDefaultAsync();

        if (!context.Loans.Any() && firstBook != null && firstMember != null)
        {
            var loan = new Loan
            {
                BookId = firstBook.Id,
                MemberId = firstMember.Id,
                LoanedAt = DateTime.UtcNow.AddDays(-7),
                DueAt = DateTime.UtcNow.AddDays(7),
                FineAmount = 0
            };
            context.Loans.Add(loan);
        }

        if (!context.InventoryItems.Any())
        {
            if (firstBook != null)
            {
                context.InventoryItems.Add(new InventoryItem { BookId = firstBook.Id, Stock = 12, Lost = 0 });
            }

            if (secondBook != null)
            {
                context.InventoryItems.Add(new InventoryItem { BookId = secondBook.Id, Stock = 8, Lost = 1 });
            }
        }

        if (!context.Acquisitions.Any())
        {
            context.Acquisitions.Add(new Acquisition
            {
                Supplier = "PT Buku Jaya",
                Title = "Data Science for Library",
                Quantity = 5,
                Cost = 1500000,
                Status = "Arrived"
            });
        }

        if (!context.AuditLogs.Any())
        {
            context.AuditLogs.Add(new AuditLog
            {
                Actor = "admin",
                Action = "Seed data",
                Module = "System"
            });
        }

        if (!context.Notifications.Any())
        {
            context.Notifications.Add(new Notification
            {
                Recipient = "member1@mail.com",
                Message = "Pengingat: Buku Sample Book 1 segera jatuh tempo.",
                Type = "Warning"
            });
        }

        if (!context.DiscussionTopics.Any())
        {
            context.DiscussionTopics.Add(new DiscussionTopic
            {
                Title = "Rekomendasi buku teknologi 2024",
                Category = "Discussion",
                Creator = "Member 1"
            });
        }

        if (!context.CommunityEvents.Any())
        {
            context.CommunityEvents.Add(new CommunityEvent
            {
                Title = "Literacy Workshop",
                Date = DateTime.UtcNow.AddDays(14),
                Location = "Main Hall"
            });
        }

        if (!context.Recommendations.Any())
        {
            context.Recommendations.Add(new Recommendation
            {
                BookTitle = "Clean Architecture",
                Reason = "Top pick untuk tim IT",
                CreatedBy = "Petugas 1"
            });
        }

        await context.SaveChangesAsync();
    }

    private static bool IsForeignKeyFailure(DbUpdateException ex)
    {
        return ex.InnerException is SqliteException sqliteException
            && sqliteException.SqliteErrorCode == 19
            && sqliteException.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase);
    }
}
