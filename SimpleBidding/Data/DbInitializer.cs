using Microsoft.AspNetCore.Identity;
using SimpleBidding.Models;
using Microsoft.EntityFrameworkCore;

namespace SimpleBidding.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            context.Database.EnsureCreated();

            // Roles
            string[] roles = { "Admin", "Seller", "Bidder" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Users
            var adminUser = await CreateUser(userManager, "admin@bidding.com", "Admin User", "Admin", "Password123!", "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?q=80&w=150&auto=format&fit=crop");
            var sellerUser = await CreateUser(userManager, "seller@bidding.com", "Seller Jacky", "Seller", "Password123!", "https://images.unsplash.com/photo-1599566150163-29194dcaad36?q=80&w=150&auto=format&fit=crop");
            var bidderUser = await CreateUser(userManager, "bidder@bidding.com", "Bidder One", "Bidder", "Password123!", "https://images.unsplash.com/photo-1527980965255-d3b416303d12?q=80&w=150&auto=format&fit=crop");

            // Items
            if (!context.AuctionItems.Any())
            {
                var items = new List<AuctionItem>
                {
                    new AuctionItem 
                    { 
                        Title = "Vintage Rolex Submariner", 
                        Description = "Jam tangan mewah legendaris dalam kondisi prima, lengkap dengan sertifikat keaslian.", 
                        StartingPrice = 150000000, 
                        CurrentPrice = 150000000, 
                        StartTime = DateTime.UtcNow, 
                        EndTime = DateTime.UtcNow.AddDays(7), 
                        Category = "Luxury", 
                        SellerId = sellerUser.Id,
                        ImageUrl = "https://images.unsplash.com/photo-1547996160-81dfa63595dd?q=80&w=600&auto=format&fit=crop"
                    },
                    new AuctionItem 
                    { 
                        Title = "Lukisan: Replika Starry Night", 
                        Description = "Replika lukisan Van Gogh yang sangat akurat menggunakan cat minyak berkualitas tinggi.", 
                        StartingPrice = 5000000, 
                        CurrentPrice = 5000000, 
                        StartTime = DateTime.UtcNow, 
                        EndTime = DateTime.UtcNow.AddDays(3), 
                        Category = "Art", 
                        SellerId = sellerUser.Id,
                        ImageUrl = "https://images.unsplash.com/photo-1541963463532-d68292c34b19?q=80&w=600&auto=format&fit=crop"
                    },
                    new AuctionItem 
                    { 
                        Title = "MacBook Pro M2 Max 64GB", 
                        Description = "Laptop performa tinggi untuk profesional kreatif dan developer.", 
                        StartingPrice = 45000000, 
                        CurrentPrice = 45000000, 
                        StartTime = DateTime.UtcNow, 
                        EndTime = DateTime.UtcNow.AddHours(5), 
                        Category = "Electronics", 
                        SellerId = sellerUser.Id,
                        ImageUrl = "https://images.unsplash.com/photo-1517336712461-481ecfbb4d0c?q=80&w=600&auto=format&fit=crop"
                    },
                    new AuctionItem 
                    { 
                        Title = "Guci Antik Dinasti Ming", 
                        Description = "Barang koleksi langka dengan ornamen naga yang sangat detail.", 
                        StartingPrice = 25000000, 
                        CurrentPrice = 25000000, 
                        StartTime = DateTime.UtcNow, 
                        EndTime = DateTime.UtcNow.AddDays(10), 
                        Category = "Antiques", 
                        SellerId = sellerUser.Id,
                        ImageUrl = "https://images.unsplash.com/photo-1610471206103-6058097d8ccb?q=80&w=600&auto=format&fit=crop"
                    }
                };
                context.AuctionItems.AddRange(items);
                await context.SaveChangesAsync();
            }
        }

        private static async Task<ApplicationUser> CreateUser(UserManager<ApplicationUser> userManager, string email, string name, string role, string password, string profilePic)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser 
                { 
                    UserName = email, 
                    Email = email, 
                    FullName = name, 
                    EmailConfirmed = true,
                    ProfilePictureUrl = profilePic
                };
                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, role);
            }
            return user;
        }
    }
}
