using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Lapak.Models;

namespace Lapak.Data;

/// <summary>
/// Main database context for Lapak e-commerce platform
/// Supports SQLite, SQL Server, MySQL, and PostgreSQL
/// </summary>
public class LapakDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public LapakDbContext(DbContextOptions<LapakDbContext> options) : base(options) { }

    // E-Commerce Tables
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ProductLike> ProductLikes => Set<ProductLike>();
    public DbSet<StoreReview> StoreReviews => Set<StoreReview>();
    public DbSet<StoreLike> StoreLikes => Set<StoreLike>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ShippingTracking> ShippingTrackings => Set<ShippingTracking>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<ProductPromo> ProductPromos => Set<ProductPromo>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category - Self-referencing hierarchy
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(e => e.ParentCategory)
                  .WithMany(e => e.SubCategories)
                  .HasForeignKey(e => e.ParentCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Price);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.StoreId);

            entity.HasOne(e => e.Category)
                  .WithMany(e => e.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Store)
                  .WithMany(e => e.Products)
                  .HasForeignKey(e => e.StoreId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Store
        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(e => e.User)
                  .WithOne(e => e.Store)
                  .HasForeignKey<Store>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CartItem - unique product per user
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(e => e.CartItems)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // WishlistItem - unique product per user
        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(e => e.WishlistItems)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductReview - one review per user per product
        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Reviews)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Product)
                  .WithMany(e => e.Reviews)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductLike - one like per user per product
        modelBuilder.Entity<ProductLike>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Product)
                  .WithMany(e => e.Likes)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // StoreReview - one review per user per store
        modelBuilder.Entity<StoreReview>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.StoreId }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Store)
                  .WithMany(e => e.Reviews)
                  .HasForeignKey(e => e.StoreId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // StoreLike - one like per user per store
        modelBuilder.Entity<StoreLike>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.StoreId }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Store)
                  .WithMany(e => e.Likes)
                  .HasForeignKey(e => e.StoreId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Orders)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Store)
                  .WithMany()
                  .HasForeignKey(e => e.StoreId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Voucher)
                  .WithMany(e => e.Orders)
                  .HasForeignKey(e => e.VoucherId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasOne(e => e.Order)
                  .WithMany(e => e.OrderItems)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany(e => e.OrderItems)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ShippingTracking
        modelBuilder.Entity<ShippingTracking>(entity =>
        {
            entity.HasOne(e => e.Order)
                  .WithMany(e => e.ShippingTrackings)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Voucher
        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // ProductPromo
        modelBuilder.Entity<ProductPromo>(entity =>
        {
            entity.HasOne(e => e.Product)
                  .WithMany(e => e.Promos)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ChatBotType);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.ChatMessages)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
