using Microsoft.EntityFrameworkCore;
using Kopdar.Models;

namespace Kopdar.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<ChatGroup> ChatGroups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    
    // Gallery
    public DbSet<GalleryFolder> GalleryFolders { get; set; }
    public DbSet<GalleryImage> GalleryImages { get; set; }
    public DbSet<GalleryLike> GalleryLikes { get; set; }
    public DbSet<GalleryComment> GalleryComments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany()
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Follow>()
            .HasOne(f => f.Following)
            .WithMany()
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Group)
            .WithMany(g => g.Messages)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
            
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
            
        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.User)
            .WithMany(u => u.GroupMemberships)
            .HasForeignKey(gm => gm.UserId)
            .OnDelete(DeleteBehavior.Restrict); // atau DeleteBehavior.NoAction
            //.OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.Group)
            .WithMany(g => g.Members)
            .HasForeignKey(gm => gm.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Gallery Config
        modelBuilder.Entity<GalleryFolder>()
            .HasOne(f => f.User)
            .WithMany(u => u.GalleryFolders)
            .HasForeignKey(f => f.UserId);

        modelBuilder.Entity<GalleryImage>()
            .HasOne(i => i.Folder)
            .WithMany(f => f.Images)
            .HasForeignKey(i => i.FolderId)
            .OnDelete(DeleteBehavior.Restrict); // atau DeleteBehavior.NoAction;

        modelBuilder.Entity<GalleryLike>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GalleryComment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
