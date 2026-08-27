using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyPoS.Models;

namespace MyPoS.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<TransactionDetail> TransactionDetails => Set<TransactionDetail>();
        public DbSet<AppSetting> Settings => Set<AppSetting>();
        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppSetting>().HasIndex(s => s.Key).IsUnique();
            modelBuilder.Entity<AppUser>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<Transaction>().HasIndex(t => t.InvoiceNumber).IsUnique();
            modelBuilder.Entity<Transaction>().HasIndex(t => t.Date);
            modelBuilder.Entity<Product>().HasIndex(p => p.Barcode);
            modelBuilder.Entity<ApiKey>().HasIndex(k => k.Prefix).IsUnique();

            // SQLite menyimpan decimal sebagai TEXT, sedangkan penyedia lain memerlukan
            // presisi eksplisit; tanpa ini SQL Server memakai decimal(18,2) diam-diam dan
            // PostgreSQL memakai numeric tanpa batas.
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(t => t.GetProperties())
                         .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(4);
            }

            // Menghapus kategori tidak boleh menghapus produk di dalamnya.
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Riwayat transaksi harus tetap utuh walau produknya dihapus dari master data.
            modelBuilder.Entity<TransactionDetail>()
                .HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Minuman" },
                new Category { Id = 2, Name = "Makanan" },
                new Category { Id = 3, Name = "Camilan" },
                new Category { Id = 4, Name = "Kebutuhan Rumah" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Coca Cola 390ml", Barcode = "8992761111014", Price = 6000, Cost = 4500, Stock = 100, MinStock = 20, CategoryId = 1, Description = "Minuman bersoda dingin", ImageUrl = "", IsActive = true },
                new Product { Id = 2, Name = "Teh Botol Sosro 350ml", Barcode = "8992761111021", Price = 5000, Cost = 3800, Stock = 80, MinStock = 20, CategoryId = 1, Description = "Teh melati dalam botol", ImageUrl = "", IsActive = true },
                new Product { Id = 3, Name = "Kopi Bubuk 200g", Barcode = "8992761111038", Price = 24000, Cost = 18000, Stock = 35, MinStock = 10, CategoryId = 1, Description = "Kopi robusta giling", ImageUrl = "/uploads/6f30da76-5263-43f9-a475-53a3b1089d07_bijikopi.jpg", IsActive = true },
                new Product { Id = 4, Name = "Indomie Goreng", Barcode = "8992761111045", Price = 3500, Cost = 2800, Stock = 200, MinStock = 40, CategoryId = 2, Description = "Mi instan goreng", ImageUrl = "", IsActive = true },
                new Product { Id = 5, Name = "Beras Pandan Wangi 5kg", Barcode = "8992761111052", Price = 78000, Cost = 68000, Stock = 24, MinStock = 8, CategoryId = 2, Description = "Beras premium", ImageUrl = "", IsActive = true },
                new Product { Id = 6, Name = "Telur Ayam 1kg", Barcode = "8992761111069", Price = 29000, Cost = 25000, Stock = 6, MinStock = 10, CategoryId = 2, Description = "Telur ayam negeri", ImageUrl = "", IsActive = true },
                new Product { Id = 7, Name = "Chitato Sapi Panggang", Barcode = "8992761111076", Price = 11000, Cost = 8500, Stock = 50, MinStock = 15, CategoryId = 3, Description = "Keripik kentang", ImageUrl = "", IsActive = true },
                new Product { Id = 8, Name = "Oreo Original 133g", Barcode = "8992761111083", Price = 9500, Cost = 7200, Stock = 45, MinStock = 15, CategoryId = 3, Description = "Biskuit sandwich cokelat", ImageUrl = "", IsActive = true },
                new Product { Id = 9, Name = "Kacang Garuda 200g", Barcode = "8992761111090", Price = 12500, Cost = 9500, Stock = 4, MinStock = 12, CategoryId = 3, Description = "Kacang kulit panggang", ImageUrl = "", IsActive = true },
                new Product { Id = 10, Name = "Sabun Cuci Piring 800ml", Barcode = "8992761111106", Price = 18500, Cost = 14000, Stock = 30, MinStock = 10, CategoryId = 4, Description = "Sabun cuci piring jeruk nipis", ImageUrl = "", IsActive = true },
                new Product { Id = 11, Name = "Tisu Wajah 250 lembar", Barcode = "8992761111113", Price = 15000, Cost = 11500, Stock = 28, MinStock = 10, CategoryId = 4, Description = "Tisu lembut 2 lapis", ImageUrl = "", IsActive = true },
                new Product { Id = 12, Name = "Minyak Goreng 2L", Barcode = "8992761111120", Price = 38000, Cost = 33000, Stock = 18, MinStock = 8, CategoryId = 4, Description = "Minyak goreng sawit", ImageUrl = "", IsActive = true }
            );

            modelBuilder.Entity<Customer>().HasData(
                new Customer { Id = 1, Name = "Budi Santoso", Phone = "081234567890", Email = "budi@mail.com", LoyaltyPoints = 50 },
                new Customer { Id = 2, Name = "Siti Rahayu", Phone = "081298765432", Email = "siti@mail.com", LoyaltyPoints = 18 },
                new Customer { Id = 3, Name = "Agus Wijaya", Phone = "081377788899", Email = "agus@mail.com", LoyaltyPoints = 0 }
            );

            // Hash PBKDF2-SHA256 100.000 iterasi dari admin123 / manager123 / operator123.
            // Nilainya dipatok agar seeding tetap deterministik; ganti kata sandi lewat
            // halaman Pengguna akan menghasilkan salt acak seperti biasa.
            modelBuilder.Entity<AppUser>().HasData(
                new AppUser
                {
                    Id = 1,
                    Username = "admin",
                    FullName = "Administrator",
                    Role = "Admin",
                    IsActive = true,
                    PasswordHash = "pbkdf2.sha256.100000.TXlQb1NTZWVkU2FsdDAwMQ==.KGjTP8lW8TOnPLCH1zyqzVsd14IRmOMWTwDjOpfrg5w="
                },
                new AppUser
                {
                    Id = 2,
                    Username = "manager",
                    FullName = "Manajer Toko",
                    Role = "Manager",
                    IsActive = true,
                    PasswordHash = "pbkdf2.sha256.100000.TXlQb1NTZWVkU2FsdDAwMg==.6/onHNL+k5BAM4gYrCFwRBJdbeUpUrevHZkUyvCr+Ck="
                },
                new AppUser
                {
                    Id = 3,
                    Username = "operator",
                    FullName = "Kasir",
                    Role = "Operator",
                    IsActive = true,
                    PasswordHash = "pbkdf2.sha256.100000.TXlQb1NTZWVkU2FsdDAwMw==.xf1rMMmP8/s1BaGTdKmmKHLm703YzHxAYdEZFQO2yXk="
                }
            );
        }
    }
}
