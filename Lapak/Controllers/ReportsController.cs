using System.Globalization;
using System.Text;
using Lapak.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lapak.Controllers;

/// <summary>
/// Report downloads. These are plain HTTP endpoints rather than Blazor callbacks
/// because a file download needs a real response the browser can save.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly LapakDbContext _db;

    public ReportsController(LapakDbContext db) => _db = db;

    /// <summary>
    /// Orders matching the dashboard filters, as CSV. Mirrors the dashboard query
    /// so the file always matches what the operator is looking at on screen.
    /// </summary>
    [HttpGet("orders.csv")]
    public async Task<IActionResult> OrdersCsv(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? tier,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var fromUtc = (from ?? DateTime.UtcNow.Date.AddDays(-29)).Date;
        var toExclusive = (to ?? DateTime.UtcNow.Date).Date.AddDays(1);

        var query = _db.Orders.AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Store)
            .Where(o => o.CreatedAt >= fromUtc && o.CreatedAt < toExclusive);

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(tier)) query = query.Where(o => o.User.Tier == tier);

        var orders = await query.OrderByDescending(o => o.CreatedAt).Take(5000).ToListAsync(ct);

        var csv = new StringBuilder();
        csv.AppendLine("Nomor Pesanan,Tanggal,Pelanggan,Email,Tier,Toko,Subtotal,Ongkir,Diskon,Total,Status,Status Bayar,Gateway,Kurir,Resi");

        foreach (var o in orders)
        {
            csv.AppendLine(string.Join(',', new[]
            {
                Csv(o.OrderNumber),
                Csv(o.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
                Csv(o.User?.FullName),
                Csv(o.User?.Email),
                Csv(o.User?.Tier),
                Csv(o.Store?.Name),
                Num(o.SubTotal),
                Num(o.ShippingCost),
                Num(o.Discount),
                Num(o.GrandTotal),
                Csv(o.Status),
                Csv(o.PaymentStatus),
                Csv(o.PaymentGateway),
                Csv(o.ShippingCourier),
                Csv(o.TrackingNumber)
            }));
        }

        // UTF-8 BOM so Excel on Windows opens the Indonesian text correctly.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        var fileName = $"lapak-pesanan-{fromUtc:yyyyMMdd}-{toExclusive.AddDays(-1):yyyyMMdd}.csv";

        return File(bytes, "text/csv", fileName);
    }

    /// <summary>Quotes a field and escapes embedded quotes, so commas in names cannot shift columns.</summary>
    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string Num(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
