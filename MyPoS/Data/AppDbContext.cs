using Microsoft.EntityFrameworkCore;
using MyPoS.Models;

namespace MyPoS.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Generate some initial categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Beverages" },
                new Category { Id = 2, Name = "Food" },
                new Category { Id = 3, Name = "Snacks" }
            );

            // Generate Products
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Coca Cola", Barcode = "1001", Price = 5000, Stock = 100, CategoryId = 1, Description = "Cold beverage", ImageUrl = "" },
                new Product { Id = 2, Name = "Pepsi", Barcode = "1002", Price = 4500, Stock = 80, CategoryId = 1, Description = "Cold beverage", ImageUrl = "" },
                new Product { Id = 3, Name = "Indomie Goreng", Barcode = "2001", Price = 3000, Stock = 200, CategoryId = 2, Description = "Instant noodle", ImageUrl = "" },
                new Product { Id = 4, Name = "Chitato", Barcode = "3001", Price = 8000, Stock = 50, CategoryId = 3, Description = "Potato chips", ImageUrl = "" }
            );

            // Generate Customers
            modelBuilder.Entity<Customer>().HasData(
                new Customer { Id = 1, Name = "Budi Santoso", Phone = "08123456789", Email = "budi@mail.com", LoyaltyPoints = 50 }
            );

            // Base64 of plain text for demo: Convert.ToBase64String(Encoding.UTF8.GetBytes("admin123")) -> YWRtaW4xMjM=
            modelBuilder.Entity<AppUser>().HasData(
                new AppUser { Id = 1, Username = "admin", PasswordHash = "YWRtaW4xMjM=", Role = "Admin" },
                new AppUser { Id = 2, Username = "operator", PasswordHash = "b3BlcmF0b3IxMjM=", Role = "Operator" },
                new AppUser { Id = 3, Username = "manager", PasswordHash = "bWFuYWdlcjEyMw==", Role = "Manager" }
            );
        }
    }
}
