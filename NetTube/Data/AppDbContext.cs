using Microsoft.EntityFrameworkCore;
using NetTube.Models;

namespace NetTube.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<CommunityPost> CommunityPosts { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistVideo> PlaylistVideos { get; set; }
        public DbSet<WatchHistory> WatchHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Subscriber)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(s => s.SubscriberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Creator)
                .WithMany()
                .HasForeignKey(s => s.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlaylistVideo>()
                .HasOne(pv => pv.Video)
                .WithMany(v => v.PlaylistVideos)
                .HasForeignKey(pv => pv.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WatchHistory>()
                .HasOne(wh => wh.Video)
                .WithMany(v => v.WatchHistories)
                .HasForeignKey(wh => wh.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<WatchHistory>()
                .HasOne(wh => wh.User)
                .WithMany(u => u.WatchHistories)
                .HasForeignKey(wh => wh.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}