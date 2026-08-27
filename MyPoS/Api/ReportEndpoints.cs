using Microsoft.EntityFrameworkCore;
using MyPoS.Data;
using MyPoS.Models;
using MyPoS.Services;
using MyPoS.Services.Payments;

namespace MyPoS.Api
{
    /// <summary>Ringkasan penjualan, penjualan per produk, stok menipis, dan info toko.</summary>
    public static class ReportEndpoints
    {
        public static RouteGroupBuilder MapReportEndpoints(this RouteGroupBuilder group)
        {
            var reports = group.MapGroup("/reports").WithTags("Laporan");

            reports.MapGet("/summary", async (
                IDbContextFactory<AppDbContext> factory,
                DateTime? from,
                DateTime? to,
                CancellationToken ct) =>
            {
                var (start, end) = ResolveRange(from, to);
                using var db = await factory.CreateDbContextAsync(ct);

                // Hanya transaksi lunas yang dihitung sebagai penjualan; yang menunggu
                // pembayaran atau sudah dibatalkan bukan pendapatan.
                var transactions = await db.Transactions
                    .Where(t => t.Status == TransactionStatus.Paid && t.Date >= start && t.Date < end)
                    .AsNoTracking()
                    .ToListAsync(ct);

                var ids = transactions.Select(t => t.Id).ToList();
                var lines = await db.TransactionDetails
                    .Where(d => ids.Contains(d.TransactionId))
                    .AsNoTracking()
                    .ToListAsync(ct);

                var revenue = lines.Sum(d => d.SubTotal);
                var cost = lines.Sum(d => d.UnitCost * d.Quantity);
                var profit = revenue - cost;

                return Results.Ok(new SalesSummaryDto(
                    From: start,
                    To: end.AddDays(-1),
                    TransactionCount: transactions.Count,
                    ItemCount: lines.Sum(d => d.Quantity),
                    Revenue: revenue,
                    Cost: cost,
                    GrossProfit: profit,
                    MarginPercent: revenue <= 0 ? 0 : Math.Round(profit / revenue * 100m, 2),
                    TaxCollected: transactions.Sum(t => t.TaxAmount),
                    DiscountGiven: transactions.Sum(t => t.DiscountAmount)));
            })
            .WithSummary("Ringkasan penjualan")
            .WithDescription("Omzet, harga pokok, laba kotor, margin, pajak terkumpul, dan diskon pada satu rentang tanggal.");

            reports.MapGet("/daily", async (
                IDbContextFactory<AppDbContext> factory,
                DateTime? from,
                DateTime? to,
                CancellationToken ct) =>
            {
                var (start, end) = ResolveRange(from, to);
                using var db = await factory.CreateDbContextAsync(ct);

                var grouped = await db.Transactions
                    .Where(t => t.Status == TransactionStatus.Paid && t.Date >= start && t.Date < end)
                    .GroupBy(t => t.Date.Date)
                    .Select(g => new { Day = g.Key, Count = g.Count(), Revenue = g.Sum(t => t.TotalAmount) })
                    .ToListAsync(ct);

                // Hari tanpa penjualan tetap dikembalikan sebagai nol agar deret waktunya utuh
                // dan pemanggil tidak perlu mengisi sendiri celahnya.
                var days = (int)(end - start).TotalDays;
                var series = Enumerable.Range(0, days)
                    .Select(offset =>
                    {
                        var day = start.AddDays(offset);
                        var match = grouped.FirstOrDefault(g => g.Day == day);
                        return new DailySalesDto(day, match?.Count ?? 0, match?.Revenue ?? 0m);
                    })
                    .ToList();

                return Results.Ok(series);
            })
            .WithSummary("Penjualan harian")
            .WithDescription("Deret harian yang sudah lengkap termasuk hari tanpa penjualan.");

            reports.MapGet("/by-product", async (
                IDbContextFactory<AppDbContext> factory,
                DateTime? from,
                DateTime? to,
                int? categoryId,
                int? top,
                CancellationToken ct) =>
            {
                var (start, end) = ResolveRange(from, to);
                using var db = await factory.CreateDbContextAsync(ct);

                var query = db.TransactionDetails
                    .Include(d => d.Product)!.ThenInclude(p => p!.Category)
                    .Where(d => d.Transaction!.Status == TransactionStatus.Paid
                                && d.Transaction.Date >= start
                                && d.Transaction.Date < end);

                if (categoryId is int cid)
                    query = query.Where(d => d.Product!.CategoryId == cid);

                var rows = await query.AsNoTracking().ToListAsync(ct);

                var result = rows
                    .GroupBy(d => new { d.ProductId, d.ProductName })
                    .Select(g =>
                    {
                        var revenue = g.Sum(d => d.SubTotal);
                        var cost = g.Sum(d => d.UnitCost * d.Quantity);
                        return new ProductSalesDto(
                            g.Key.ProductId,
                            g.Key.ProductName,
                            g.First().Product?.Category?.Name ?? "Tanpa kategori",
                            g.Sum(d => d.Quantity),
                            revenue,
                            cost,
                            revenue - cost);
                    })
                    .OrderByDescending(r => r.Revenue)
                    .Take(Math.Clamp(top ?? 100, 1, 500))
                    .ToList();

                return Results.Ok(result);
            })
            .WithSummary("Penjualan per produk")
            .WithDescription("Diurutkan dari omzet terbesar, lengkap dengan harga pokok dan laba kotor per produk.");

            reports.MapGet("/low-stock", async (
                IDbContextFactory<AppDbContext> factory,
                SettingsService settingsService,
                CancellationToken ct) =>
            {
                var settings = await settingsService.GetAsync();
                using var db = await factory.CreateDbContextAsync(ct);

                var products = await db.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .AsNoTracking()
                    .ToListAsync(ct);

                var low = products
                    .Select(p => new { Product = p, Threshold = p.MinStock > 0 ? p.MinStock : settings.LowStockThreshold })
                    .Where(x => x.Product.Stock <= x.Threshold)
                    .OrderBy(x => x.Product.Stock)
                    .Select(x => new LowStockDto(
                        x.Product.Id, x.Product.Name, x.Product.Category?.Name, x.Product.Stock, x.Threshold))
                    .ToList();

                return Results.Ok(low);
            })
            .WithSummary("Produk yang perlu diisi ulang")
            .WithDescription("Memakai ambang per produk bila ada, atau ambang bawaan dari Pengaturan.");

            reports.MapGet("/store-info", async (
                SettingsService settingsService,
                PaymentGatewayResolver gateways,
                CancellationToken ct) =>
            {
                var settings = await settingsService.GetAsync();

                return Results.Ok(new StoreInfoDto(
                    settings.StoreName,
                    settings.CurrencyCode,
                    settings.CurrencySymbol,
                    settings.CurrencyDecimals,
                    settings.TaxEnabled,
                    settings.TaxName,
                    settings.TaxRatePercent,
                    settings.TaxInclusive,
                    gateways.Enabled(settings).Select(g => g.Name).ToList()));
            })
            .WithSummary("Informasi toko dan aturan yang berlaku")
            .WithDescription("Dipakai aplikasi luar untuk memformat nominal dan menampilkan pilihan pembayaran yang sama dengan kasir.");

            return group;
        }

        /// <summary>Bawaannya 30 hari terakhir bila rentang tidak diberikan.</summary>
        private static (DateTime Start, DateTime End) ResolveRange(DateTime? from, DateTime? to)
        {
            var start = (from ?? DateTime.Today.AddDays(-29)).Date;
            var end = (to ?? DateTime.Today).Date.AddDays(1);
            return end <= start ? (start, start.AddDays(1)) : (start, end);
        }
    }
}
