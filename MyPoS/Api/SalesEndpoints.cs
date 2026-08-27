using Microsoft.EntityFrameworkCore;
using MyPoS.Data;
using MyPoS.Models;
using MyPoS.Services;

namespace MyPoS.Api
{
    /// <summary>Transaksi: pembacaan riwayat, pembuatan, dan pembatalan.</summary>
    public static class SalesEndpoints
    {
        public static RouteGroupBuilder MapSalesEndpoints(this RouteGroupBuilder group)
        {
            var sales = group.MapGroup("/transactions").WithTags("Transaksi");

            sales.MapGet("/", async (
                IDbContextFactory<AppDbContext> factory,
                DateTime? from,
                DateTime? to,
                string? status,
                string? search,
                int? page,
                int? pageSize,
                CancellationToken ct) =>
            {
                var (p, size) = ApiRoutes.ReadPaging(page, pageSize);
                using var db = await factory.CreateDbContextAsync(ct);

                var query = db.Transactions.Include(t => t.Customer).AsNoTracking().AsQueryable();

                if (from is DateTime f) query = query.Where(t => t.Date >= f.Date);
                if (to is DateTime t2) query = query.Where(t => t.Date < t2.Date.AddDays(1));

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (!Enum.TryParse<TransactionStatus>(status, ignoreCase: true, out var parsed))
                        return Results.BadRequest(new ApiError($"Status \"{status}\" tidak dikenal. Pilihan: {string.Join(", ", Enum.GetNames<TransactionStatus>())}."));

                    query = query.Where(t => t.Status == parsed);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(t =>
                        EF.Functions.Like(t.InvoiceNumber, $"%{search}%") ||
                        EF.Functions.Like(t.CashierName, $"%{search}%"));
                }

                var total = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(t => t.Date)
                    .Skip((p - 1) * size)
                    .Take(size)
                    .ToListAsync(ct);

                // Baris rincian ditarik terpisah supaya kueri daftar tidak menggandakan
                // baris transaksi lewat join.
                var ids = items.Select(t => t.Id).ToList();
                var lines = await db.TransactionDetails
                    .Where(d => ids.Contains(d.TransactionId))
                    .AsNoTracking()
                    .ToListAsync(ct);

                var lookup = lines.GroupBy(d => d.TransactionId).ToDictionary(g => g.Key, g => g.ToList());

                var dtos = items
                    .Select(t => Map(t, lookup.TryGetValue(t.Id, out var l) ? l : []))
                    .ToList();

                return Results.Ok(ApiRoutes.ToPaged(dtos, total, p, size));
            })
            .WithSummary("Daftar transaksi")
            .WithDescription("Saring dengan rentang tanggal, status (Pending/Paid/Failed/Voided/Refunded), dan pencarian invoice atau kasir.");

            sales.MapGet("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                using var db = await factory.CreateDbContextAsync(ct);
                var transaction = await db.Transactions
                    .Include(t => t.Customer)
                    .Include(t => t.Details)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id, ct);

                return transaction is null
                    ? Results.NotFound(new ApiError($"Transaksi {id} tidak ditemukan."))
                    : Results.Ok(Map(transaction, transaction.Details.ToList()));
            })
            .WithSummary("Ambil satu transaksi beserta rinciannya");

            sales.MapGet("/invoice/{invoiceNumber}", async (string invoiceNumber, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                using var db = await factory.CreateDbContextAsync(ct);
                var transaction = await db.Transactions
                    .Include(t => t.Customer)
                    .Include(t => t.Details)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.InvoiceNumber == invoiceNumber, ct);

                return transaction is null
                    ? Results.NotFound(new ApiError($"Invoice {invoiceNumber} tidak ditemukan."))
                    : Results.Ok(Map(transaction, transaction.Details.ToList()));
            })
            .WithSummary("Ambil transaksi berdasarkan nomor invoice");

            sales.MapPost("/", async (
                CheckoutRequestDto body,
                HttpContext http,
                IDbContextFactory<AppDbContext> factory,
                CheckoutService checkout,
                CancellationToken ct) =>
            {
                if (body.Lines is null || body.Lines.Count == 0)
                    return Results.BadRequest(new ApiError("Minimal satu baris barang wajib diisi."));

                using var db = await factory.CreateDbContextAsync(ct);

                var productIds = body.Lines.Select(l => l.ProductId).Distinct().ToList();
                var products = await db.Products
                    .Where(p => productIds.Contains(p.Id))
                    .AsNoTracking()
                    .ToDictionaryAsync(p => p.Id, ct);

                var cart = new List<CartLine>();
                foreach (var line in body.Lines)
                {
                    if (!products.TryGetValue(line.ProductId, out var product))
                        return Results.BadRequest(new ApiError($"Produk {line.ProductId} tidak ditemukan."));

                    if (line.Quantity <= 0)
                        return Results.BadRequest(new ApiError($"Jumlah untuk produk {line.ProductId} harus lebih dari nol."));

                    cart.Add(new CartLine
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = line.Quantity,
                        // Harga boleh ditimpa pemanggil, mis. untuk harga grosir; bila tidak
                        // diisi, dipakai harga jual yang berlaku saat ini.
                        UnitPrice = line.UnitPrice ?? product.Price,
                        UnitCost = product.Cost,
                        DiscountAmount = line.DiscountAmount,
                        AvailableStock = product.Stock
                    });
                }

                var apiKey = http.GetApiKey();
                var result = await checkout.CheckoutAsync(new CheckoutRequest
                {
                    Lines = cart,
                    CashierName = string.IsNullOrWhiteSpace(body.CashierName)
                        ? $"API: {apiKey?.Name ?? "tidak dikenal"}"
                        : body.CashierName.Trim(),
                    PaymentProvider = body.PaymentProvider ?? "Cash",
                    PaymentMethod = body.PaymentProvider ?? "Cash",
                    CustomerId = body.CustomerId,
                    OrderDiscountAmount = body.OrderDiscountAmount,
                    OrderDiscountPercent = body.OrderDiscountPercent,
                    PaidAmount = body.PaidAmount,
                    Notes = body.Notes,
                    ReturnUrlBase = $"{http.Request.Scheme}://{http.Request.Host}"
                }, ct);

                if (!result.Success)
                    return Results.BadRequest(new CheckoutResponseDto(false, null, null, result.Error));

                using var readback = await factory.CreateDbContextAsync(ct);
                var saved = await readback.Transactions
                    .Include(t => t.Customer)
                    .Include(t => t.Details)
                    .AsNoTracking()
                    .FirstAsync(t => t.Id == result.Transaction!.Id, ct);

                return Results.Created(
                    $"/api/v1/transactions/{saved.Id}",
                    new CheckoutResponseDto(true, Map(saved, saved.Details.ToList()), result.RedirectUrl, null));
            })
            .WithSummary("Buat transaksi baru")
            .WithDescription(
                "Menjalankan alur yang sama persis dengan halaman kasir: validasi stok, perhitungan " +
                "pajak, pemanggilan penyedia pembayaran, pengurangan stok, dan poin loyalitas. " +
                "Memerlukan kunci API dengan izin tulis.");

            sales.MapPost("/{id:int}/void", async (
                int id,
                VoidRequestDto body,
                CheckoutService checkout,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(body.Reason))
                    return Results.BadRequest(new ApiError("Alasan pembatalan wajib diisi."));

                var ok = await checkout.VoidAsync(id, body.Reason.Trim(), ct);
                return ok
                    ? Results.Ok(new { id, voided = true })
                    : Results.BadRequest(new ApiError("Transaksi tidak ditemukan atau sudah dibatalkan."));
            })
            .WithSummary("Batalkan transaksi")
            .WithDescription("Mengembalikan stok dan menarik kembali poin loyalitas yang sudah diberikan.");

            sales.MapPost("/{id:int}/refresh-status", async (int id, CheckoutService checkout, CancellationToken ct) =>
            {
                var status = await checkout.RefreshStatusAsync(id, ct);
                return Results.Ok(new { id, status = status.ToString() });
            })
            .WithSummary("Tanyakan ulang status pembayaran ke penyedia");

            return group;
        }

        internal static TransactionDto Map(Transaction t, IReadOnlyList<TransactionDetail> lines) => new(
            t.Id,
            t.InvoiceNumber,
            t.Date,
            t.Status.ToString(),
            t.CashierName,
            t.CustomerId,
            t.Customer?.Name,
            t.PaymentMethod,
            t.PaymentProvider,
            t.SubTotal,
            t.DiscountAmount,
            t.TaxableAmount,
            t.TaxAmount,
            t.ServiceChargeAmount,
            t.RoundingAmount,
            t.TotalAmount,
            t.PaidAmount,
            t.ChangeAmount,
            t.TaxRate,
            t.TaxInclusive,
            lines.Select(d => new TransactionLineDto(
                d.ProductId, d.ProductName, d.Quantity, d.UnitPrice, d.DiscountAmount, d.SubTotal)).ToList());
    }
}
