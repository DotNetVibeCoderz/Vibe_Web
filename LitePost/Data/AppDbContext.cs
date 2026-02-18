using Microsoft.EntityFrameworkCore;
using LitePost.Data.Models;

namespace LitePost.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Like> Likes { get; set; } = null!;
        public DbSet<Follow> Follows { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Bookmark> Bookmarks { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<UserList> UserLists { get; set; } = null!;
        public DbSet<UserListMember> UserListMembers { get; set; } = null!;
        public DbSet<Poll> Polls { get; set; } = null!;
        public DbSet<PollOption> PollOptions { get; set; } = null!;
        public DbSet<PollVote> PollVotes { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Likes
            modelBuilder.Entity<Like>()
                .HasKey(l => new { l.UserId, l.PostId });

            modelBuilder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Bookmarks
            modelBuilder.Entity<Bookmark>()
                .HasKey(b => new { b.UserId, b.PostId });

            modelBuilder.Entity<Bookmark>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Bookmark>()
                .HasOne(b => b.Post)
                .WithMany()
                .HasForeignKey(b => b.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Follows
            modelBuilder.Entity<Follow>()
                .HasKey(f => new { f.FollowerId, f.FollowingId });

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany() 
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Following)
                .WithMany()
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Messages
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Posts
            modelBuilder.Entity<Post>()
                .HasOne(p => p.ParentPost)
                .WithMany(p => p.Replies)
                .HasForeignKey(p => p.ParentPostId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Post>()
                .HasOne(p => p.QuotePost)
                .WithMany()
                .HasForeignKey(p => p.QuotePostId)
                .OnDelete(DeleteBehavior.ClientSetNull);
                
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Poll)
                .WithOne(p => p.Post)
                .HasForeignKey<Poll>(p => p.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Notifications
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Actor)
                .WithMany()
                .HasForeignKey(n => n.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            // User Lists
            modelBuilder.Entity<UserList>()
                .HasOne(ul => ul.Owner)
                .WithMany()
                .HasForeignKey(ul => ul.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserListMember>()
                .HasKey(ulm => new { ulm.ListId, ulm.UserId });

            modelBuilder.Entity<UserListMember>()
                .HasOne(ulm => ulm.UserList)
                .WithMany(ul => ul.Members)
                .HasForeignKey(ulm => ulm.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserListMember>()
                .HasOne(ulm => ulm.User)
                .WithMany()
                .HasForeignKey(ulm => ulm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Polls
            modelBuilder.Entity<PollOption>()
                .HasOne(po => po.Poll)
                .WithMany(p => p.Options)
                .HasForeignKey(po => po.PollId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<PollVote>()
                .HasOne(pv => pv.PollOption)
                .WithMany(po => po.Votes)
                .HasForeignKey(pv => pv.PollOptionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Password Reset Tokens
            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}