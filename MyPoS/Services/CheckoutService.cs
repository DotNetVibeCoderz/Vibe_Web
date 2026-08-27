using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyPoS.Data;
using MyPoS.Models;
using MyPoS.Services.Payments;

namespace MyPoS.Services
{
    /// <summary>Baris keranjang di halaman kasir.</summary>
    public class CartLine : ICartLine
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public int AvailableStock { get; set; }

        public decimal LineTotal => (UnitPrice * Quantity) - DiscountAmount;
    }

    public record CheckoutRequest
    {
        public required IReadOnlyList<CartLine> Lines { get; init; }
        public required string CashierName { get; init; }
        public string PaymentProvider { get; init; } = "Cash";
        public string PaymentMethod { get; init; } = "Tunai";
        public int? CustomerId { get; init; }
        public decimal OrderDiscountAmount { get; init; }
        public decimal OrderDiscountPercent { get; init; }
        public decimal PaidAmount { get; init; }
        public string? Notes { get; init; }
        public string? ReturnUrlBase { get; init; }
    }

    public record CheckoutResult
    {
        public required bool Success { get; init; }
        public Transaction? Transaction { get; init; }
        public string? Error { get; init; }
        /// <summary>Diisi bila pelanggan perlu diarahkan ke halaman pembayaran gateway.</summary>
        public string? RedirectUrl { get; init; }

        public static CheckoutResult Fail(string error) => new() { Success = false, Error = error };
    }

    /// <summary>
    /// Menyatukan pembuatan transaksi: validasi stok, perhitungan total, pemanggilan gateway,
    /// pengurangan stok, dan poin loyalitas. Sebelumnya logika ini tersebar di POS.razor dan
    /// diam-diam membuang baris yang stoknya kurang sementara totalnya tetap ditagih penuh.
    /// </summary>
    public class CheckoutService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly SettingsService _settingsService;
        private readonly PaymentGatewayResolver _gateways;

        public CheckoutService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            SettingsService settingsService,
            PaymentGatewayResolver gateways)
        {
            _dbContextFactory = dbContextFactory;
            _settingsService = settingsService;
            _gateways = gateways;
        }

        public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
        {
            if (request.Lines.Count == 0)
                return CheckoutResult.Fail("Keranjang masih kosong.");

            var settings = await _settingsService.GetAsync();
            var totals = TaxCalculator.Compute(request.Lines, settings, request.OrderDiscountAmount, request.OrderDiscountPercent);
            var gateway = _gateways.FindOrCash(request.PaymentProvider);

            if (!gateway.IsConfigured(settings))
                return CheckoutResult.Fail($"Metode pembayaran {gateway.DisplayName} belum dikonfigurasi di Pengaturan.");

            if (!gateway.RequiresRedirect && request.PaidAmount < totals.Total)
                return CheckoutResult.Fail("Uang yang dibayarkan kurang dari total tagihan.");

            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            await using var dbTx = await db.Database.BeginTransactionAsync(ct);

            // Stok dibaca ulang di dalam transaksi database; nilai di layar bisa saja basi.
            var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
            var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

            foreach (var line in request.Lines)
            {
                if (!products.TryGetValue(line.ProductId, out var product))
                    return CheckoutResult.Fail($"Produk \"{line.ProductName}\" sudah tidak tersedia.");

                if (settings.BlockSaleWhenOutOfStock && product.Stock < line.Quantity)
                    return CheckoutResult.Fail($"Stok \"{product.Name}\" tinggal {product.Stock}, diminta {line.Quantity}.");
            }

            var transaction = new Transaction
            {
                InvoiceNumber = await NextInvoiceNumberAsync(db, settings, ct),
                Date = DateTime.Now,
                SubTotal = totals.LineSubTotal,
                DiscountAmount = totals.TotalDiscount,
                TaxableAmount = totals.TaxableAmount,
                TaxAmount = totals.TaxAmount,
                ServiceChargeAmount = totals.ServiceCharge,
                RoundingAmount = totals.Rounding,
                TotalAmount = totals.Total,
                TaxRate = settings.TaxEnabled ? settings.TaxRatePercent : 0m,
                TaxInclusive = settings.TaxInclusive,
                PaymentMethod = request.PaymentMethod,
                PaymentProvider = gateway.Name,
                CashierName = request.CashierName,
                CustomerId = request.CustomerId,
                Notes = request.Notes,
                Status = TransactionStatus.Pending
            };

            foreach (var line in request.Lines)
            {
                var product = products[line.ProductId];
                transaction.Details.Add(new TransactionDetail
                {
                    ProductId = line.ProductId,
                    ProductName = product.Name,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    UnitCost = product.Cost,
                    DiscountAmount = line.DiscountAmount,
                    SubTotal = line.LineTotal
                });
            }

            var payment = await gateway.CreatePaymentAsync(new PaymentRequest
            {
                InvoiceNumber = transaction.InvoiceNumber,
                Amount = totals.Total,
                CurrencyCode = settings.CurrencyCode,
                CustomerName = request.CustomerId is null ? null : (await db.Customers.FindAsync([request.CustomerId], ct))?.Name,
                CustomerEmail = request.CustomerId is null ? null : (await db.Customers.FindAsync([request.CustomerId], ct))?.Email,
                SuccessUrl = BuildReturnUrl(request.ReturnUrlBase, settings, "sukses"),
                FailureUrl = BuildReturnUrl(request.ReturnUrlBase, settings, "gagal"),
                Items = request.Lines.Select(l => new PaymentItem(l.ProductName, l.Quantity, l.UnitPrice)).ToList()
            }, settings, ct);

            if (!payment.Success)
                return CheckoutResult.Fail(payment.Message ?? "Pembayaran ditolak penyedia.");

            transaction.PaymentReference = payment.Reference;
            transaction.PaymentUrl = payment.RedirectUrl;
            transaction.Status = payment.State == PaymentState.Paid ? TransactionStatus.Paid : TransactionStatus.Pending;

            if (transaction.Status == TransactionStatus.Paid)
            {
                transaction.PaidAmount = gateway.RequiresRedirect ? totals.Total : request.PaidAmount;
                transaction.ChangeAmount = transaction.PaidAmount - totals.Total;
            }

            db.Transactions.Add(transaction);

            // Stok dikurangi begitu transaksi dibuat supaya barang tidak terjual dua kali
            // saat pelanggan masih di halaman pembayaran. Void akan mengembalikannya.
            foreach (var line in request.Lines)
                products[line.ProductId].Stock -= line.Quantity;

            if (transaction.Status == TransactionStatus.Paid)
                await AwardLoyaltyAsync(db, transaction, settings, ct);

            await db.SaveChangesAsync(ct);
            await dbTx.CommitAsync(ct);

            return new CheckoutResult
            {
                Success = true,
                Transaction = transaction,
                RedirectUrl = payment.RedirectUrl
            };
        }

        /// <summary>
        /// Dipakai endpoint webhook: isi notifikasi hanya dipakai untuk mengetahui invoice mana
        /// yang berubah, statusnya tetap ditanyakan langsung ke gateway agar payload palsu
        /// tidak dapat menandai transaksi sebagai lunas.
        /// </summary>
        public async Task<TransactionStatus?> RefreshStatusByInvoiceAsync(string invoiceNumber, CancellationToken ct = default)
        {
            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            var id = await db.Transactions
                .Where(t => t.InvoiceNumber == invoiceNumber)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync(ct);

            if (id is null) return null;
            return await RefreshStatusAsync(id.Value, ct);
        }

        /// <summary>Menanyakan status terkini ke gateway dan menyimpannya bila berubah.</summary>
        public async Task<TransactionStatus> RefreshStatusAsync(int transactionId, CancellationToken ct = default)
        {
            var settings = await _settingsService.GetAsync();
            using var db = await _dbContextFactory.CreateDbContextAsync(ct);

            var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId, ct);
            if (transaction is null) return TransactionStatus.Failed;
            if (transaction.Status != TransactionStatus.Pending) return transaction.Status;
            if (string.IsNullOrWhiteSpace(transaction.PaymentReference)) return transaction.Status;

            var gateway = _gateways.FindOrCash(transaction.PaymentProvider);
            var result = await gateway.CheckStatusAsync(transaction.PaymentReference, settings, ct);
            if (!result.Success) return transaction.Status;

            transaction.Status = result.State switch
            {
                PaymentState.Paid => TransactionStatus.Paid,
                PaymentState.Failed or PaymentState.Expired => TransactionStatus.Failed,
                _ => TransactionStatus.Pending
            };

            if (transaction.Status == TransactionStatus.Paid)
            {
                transaction.PaidAmount = transaction.TotalAmount;
                await AwardLoyaltyAsync(db, transaction, settings, ct);
            }
            else if (transaction.Status == TransactionStatus.Failed)
            {
                await RestoreStockAsync(db, transaction.Id, ct);
            }

            await db.SaveChangesAsync(ct);
            return transaction.Status;
        }

        /// <summary>Membatalkan transaksi dan mengembalikan stok.</summary>
        public async Task<bool> VoidAsync(int transactionId, string reason, CancellationToken ct = default)
        {
            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            await using var dbTx = await db.Database.BeginTransactionAsync(ct);

            var transaction = await db.Transactions
                .Include(t => t.Details)
                .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

            if (transaction is null) return false;
            if (transaction.Status is TransactionStatus.Voided or TransactionStatus.Refunded) return false;

            var wasPaid = transaction.Status == TransactionStatus.Paid;
            transaction.Status = wasPaid ? TransactionStatus.Refunded : TransactionStatus.Voided;
            transaction.Notes = string.IsNullOrWhiteSpace(transaction.Notes)
                ? $"Dibatalkan: {reason}"
                : $"{transaction.Notes} | Dibatalkan: {reason}";

            await RestoreStockAsync(db, transaction.Id, ct);

            // Poin loyalitas yang sudah diberikan ikut ditarik kembali.
            var settings = await _settingsService.GetAsync();
            if (wasPaid && settings.LoyaltyEnabled && transaction.CustomerId is int customerId)
            {
                var customer = await db.Customers.FindAsync([customerId], ct);
                if (customer is not null)
                    customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints - PointsFor(transaction.TotalAmount, settings));
            }

            await db.SaveChangesAsync(ct);
            await dbTx.CommitAsync(ct);
            return true;
        }

        private static async Task RestoreStockAsync(AppDbContext db, int transactionId, CancellationToken ct)
        {
            var details = await db.TransactionDetails.Where(d => d.TransactionId == transactionId).ToListAsync(ct);
            var ids = details.Select(d => d.ProductId).Distinct().ToList();
            var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

            foreach (var detail in details)
            {
                if (products.TryGetValue(detail.ProductId, out var product))
                    product.Stock += detail.Quantity;
            }
        }

        private static async Task AwardLoyaltyAsync(AppDbContext db, Transaction transaction, PosSettings settings, CancellationToken ct)
        {
            if (!settings.LoyaltyEnabled || transaction.CustomerId is not int customerId) return;

            var customer = await db.Customers.FindAsync([customerId], ct);
            if (customer is null) return;

            customer.LoyaltyPoints += PointsFor(transaction.TotalAmount, settings);
        }

        private static int PointsFor(decimal amount, PosSettings settings)
            => settings.LoyaltyAmountPerPoint <= 0 ? 0 : (int)(amount / settings.LoyaltyAmountPerPoint);

        /// <summary>
        /// Nomor invoice berurutan per hari, mis. INV-20260827-0007.
        ///
        /// Urutannya diambil dari nomor tertinggi yang sudah ada, bukan dari jumlah baris.
        /// Menghitung baris membuat nomor berikutnya menabrak nomor yang sudah terpakai
        /// begitu ada satu transaksi yang dihapus dari basis data.
        ///
        /// Bila dua kasir menekan Bayar pada saat yang sama, indeks unik pada InvoiceNumber
        /// tetap menjadi pengaman terakhir: salah satunya gagal menyimpan dan dapat diulang.
        /// </summary>
        private static async Task<string> NextInvoiceNumberAsync(AppDbContext db, PosSettings settings, CancellationToken ct)
        {
            var prefix = string.IsNullOrWhiteSpace(settings.InvoicePrefix) ? "INV" : settings.InvoicePrefix.Trim();
            var dayPrefix = $"{prefix}-{DateTime.Today:yyyyMMdd}-";

            var issuedToday = await db.Transactions
                .Where(t => t.InvoiceNumber.StartsWith(dayPrefix))
                .Select(t => t.InvoiceNumber)
                .ToListAsync(ct);

            var highest = issuedToday
                .Select(number => int.TryParse(number[dayPrefix.Length..], out var sequence) ? sequence : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"{dayPrefix}{highest + 1:D4}";
        }

        private static string? BuildReturnUrl(string? requestBase, PosSettings settings, string outcome)
        {
            var baseUrl = !string.IsNullOrWhiteSpace(settings.PublicBaseUrl) ? settings.PublicBaseUrl : requestBase;
            if (string.IsNullOrWhiteSpace(baseUrl)) return null;
            return $"{baseUrl.TrimEnd('/')}/pembayaran/{outcome}";
        }
    }
}
