using Microsoft.EntityFrameworkCore;
using SportTracker.Models;
using System.Security.Cryptography;
using System.Text;

namespace SportTracker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Club> Clubs { get; set; }
        public DbSet<UserClub> UserClubs { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Goal> Goals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserClub>()
                .HasKey(uc => new { uc.UserId, uc.ClubId });

            modelBuilder.Entity<Follow>()
                .HasKey(f => new { f.FollowerId, f.FollowedId });

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Followed)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Activity>()
                .HasOne(a => a.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var adminPassword = HashPassword("admin123");
            var userPassword = HashPassword("user123");

            // Seed Users
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", PasswordHash = adminPassword, Role = "admin", Email = "admin@tracker.com", ProfileImageUrl = "https://ui-avatars.com/api/?name=Admin+User&background=random" },
                new User { Id = 2, Username = "jane_doe", PasswordHash = userPassword, Role = "user", Email = "jane@example.com", ProfileImageUrl = "https://ui-avatars.com/api/?name=Jane+Doe&background=random" },
                new User { Id = 3, Username = "john_smith", PasswordHash = userPassword, Role = "user", Email = "john@example.com", ProfileImageUrl = "https://ui-avatars.com/api/?name=John+Smith&background=random" },
                new User { Id = 4, Username = "budi_racer", PasswordHash = userPassword, Role = "user", Email = "budi@example.com", ProfileImageUrl = "https://ui-avatars.com/api/?name=Budi+Racer&background=random" },
                new User { Id = 5, Username = "siti_runner", PasswordHash = userPassword, Role = "user", Email = "siti@example.com", ProfileImageUrl = "https://ui-avatars.com/api/?name=Siti+Runner&background=random" }
            );

            // Seed Follows
            modelBuilder.Entity<Follow>().HasData(
                new Follow { FollowerId = 2, FollowedId = 3 },
                new Follow { FollowerId = 3, FollowedId = 2 },
                new Follow { FollowerId = 4, FollowedId = 2 },
                new Follow { FollowerId = 5, FollowedId = 3 }
            );

            // Seed Clubs
            modelBuilder.Entity<Club>().HasData(
                new Club { Id = 1, Name = "Weekend Warriors", Description = "For those who run and ride on weekends", CoverImageUrl = "https://images.unsplash.com/photo-1552674605-db6ffd4facb5?w=500" },
                new Club { Id = 2, Name = "Jakarta Cyclists", Description = "Cycling enthusiasts in Jakarta", CoverImageUrl = "https://images.unsplash.com/photo-1517649763962-0c623066013b?w=500" }
            );

            // Seed UserClubs
            modelBuilder.Entity<UserClub>().HasData(
                new UserClub { UserId = 2, ClubId = 1 },
                new UserClub { UserId = 3, ClubId = 1 },
                new UserClub { UserId = 4, ClubId = 2 },
                new UserClub { UserId = 5, ClubId = 1 }
            );

            // Seed Activities
            var rnd = new Random(123);
            int activityId = 1;
            for (int u = 2; u <= 5; u++)
            {
                for (int i = 1; i <= 5; i++)
                {
                    double distance = rnd.Next(3, 25) + rnd.NextDouble();
                    int durationMins = (int)(distance * rnd.Next(4, 8)); // roughly 4-8 mins per km
                    modelBuilder.Entity<Activity>().HasData(new Activity
                    {
                        Id = activityId,
                        UserId = u,
                        Title = $"Morning {(u % 2 == 0 ? "Run" : "Ride")}",
                        Type = u % 2 == 0 ? "Run" : "Ride",
                        DistanceKm = Math.Round(distance, 2),
                        Duration = TimeSpan.FromMinutes(durationMins),
                        ElevationGainMeters = rnd.Next(10, 500),
                        CaloriesBurned = durationMins * rnd.Next(8, 15),
                        AverageHeartRate = rnd.Next(130, 180),
                        AveragePaceMinPerKm = durationMins / distance,
                        Date = DateTime.UtcNow.AddDays(-rnd.Next(1, 30)).AddHours(rnd.Next(-10, 10)),
                        Description = "Feeling great!",
                        MapRouteData = "[[-6.200000, 106.816666], [-6.205000, 106.820000], [-6.210000, 106.825000]]" // Mock polyline
                    });
                    activityId++;
                }
            }

            // Seed Goals
            modelBuilder.Entity<Goal>().HasData(
                new Goal { Id = 1, UserId = 2, Type = "Distance", ActivityType = "Run", TargetValue = 50, CurrentValue = 15, StartDate = DateTime.UtcNow.AddDays(-5), EndDate = DateTime.UtcNow.AddDays(25) }
            );
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}