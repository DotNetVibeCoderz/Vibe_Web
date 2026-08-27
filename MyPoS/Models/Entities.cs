using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyPoS.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required, MaxLength(120)]
        public string Name { get; set; } = "";
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class Product
    {
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string Name { get; set; } = "";
        // Kolom yang diberi indeks wajib punya panjang maksimum: MySQL tidak dapat
        // mengindeks kolom teks tanpa batas panjang.
        [MaxLength(64)]
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        /// <summary>Harga modal / cost of goods, dipakai untuk laporan margin.</summary>
        public decimal Cost { get; set; }
        public int Stock { get; set; }
        /// <summary>Ambang stok menipis khusus produk ini. 0 = pakai default dari Pengaturan.</summary>
        public int MinStock { get; set; }
        public bool IsActive { get; set; } = true;
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }

    public enum TransactionStatus
    {
        Pending = 0,
        Paid = 1,
        Failed = 2,
        Voided = 3,
        Refunded = 4
    }

    public class Transaction
    {
        public int Id { get; set; }
        [MaxLength(64)]
        public string InvoiceNumber { get; set; } = "";
        public DateTime Date { get; set; }

        /// <summary>Jumlah seluruh baris sebelum diskon dan pajak.</summary>
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        /// <summary>Dasar Pengenaan Pajak = SubTotal - Diskon (untuk pajak exclusive).</summary>
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ServiceChargeAmount { get; set; }
        public decimal RoundingAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }

        /// <summary>Snapshot tarif pajak saat transaksi dibuat, agar laporan lama tidak berubah saat tarif diubah.</summary>
        public decimal TaxRate { get; set; }
        public bool TaxInclusive { get; set; }

        [MaxLength(64)]
        public string PaymentMethod { get; set; } = "Cash";
        /// <summary>Cash | Xendit | Midtrans | Stripe</summary>
        [MaxLength(32)]
        public string PaymentProvider { get; set; } = "Cash";
        [MaxLength(128)]
        public string? PaymentReference { get; set; }
        [MaxLength(512)]
        public string? PaymentUrl { get; set; }
        public TransactionStatus Status { get; set; } = TransactionStatus.Paid;

        [MaxLength(120)]
        public string CashierName { get; set; } = "";
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public string? Notes { get; set; }

        public ICollection<TransactionDetail> Details { get; set; } = new List<TransactionDetail>();
    }

    public class TransactionDetail
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public Transaction? Transaction { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        /// <summary>Nama produk disalin saat transaksi, supaya struk lama tetap benar bila produk dihapus/diubah.</summary>
        [MaxLength(200)]
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Name { get; set; } = "";
        [MaxLength(40)]
        public string? Phone { get; set; }
        [MaxLength(200)]
        public string? Email { get; set; }
        public int LoyaltyPoints { get; set; }
    }

    public class AppUser
    {
        public int Id { get; set; }
        [Required, MaxLength(64)]
        public string Username { get; set; } = "";
        [Required, MaxLength(256)]
        public string PasswordHash { get; set; } = "";
        [MaxLength(32)]
        public string Role { get; set; } = "Operator";
        [MaxLength(150)]
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Penyimpanan konfigurasi key-value. Dipakai halaman Pengaturan supaya nilai yang
    /// sering berubah (pajak, mata uang, identitas toko, kredensial payment gateway)
    /// tidak perlu di-hardcode maupun ikut mengubah skema database.
    /// </summary>
    public class AppSetting
    {
        public int Id { get; set; }
        [Required, MaxLength(128)]
        public string Key { get; set; } = "";
        [MaxLength(2000)]
        public string Value { get; set; } = "";
    }

    /// <summary>
    /// Kunci akses REST API. Nilai kunci penuh hanya diperlihatkan satu kali saat dibuat;
    /// yang tersimpan hanyalah hash-nya, dengan pola yang sama seperti kata sandi pengguna.
    /// </summary>
    public class ApiKey
    {
        public int Id { get; set; }
        [Required, MaxLength(120)]
        public string Name { get; set; } = "";
        /// <summary>Delapan karakter pertama kunci, dipakai untuk pencarian cepat dan penanda di antarmuka.</summary>
        [Required, MaxLength(16)]
        public string Prefix { get; set; } = "";
        [Required, MaxLength(256)]
        public string KeyHash { get; set; } = "";
        /// <summary>false = hanya boleh membaca. Endpoint yang mengubah data akan ditolak.</summary>
        public bool CanWrite { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
