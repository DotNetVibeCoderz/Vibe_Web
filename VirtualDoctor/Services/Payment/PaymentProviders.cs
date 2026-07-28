using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services.Payment;

/// <summary>Data yang dikembalikan penyedia saat tagihan dibuat.</summary>
public record ChargeResult(
    string Provider,
    PaymentChannel Channel,
    string? ExternalId = null,
    string? QrPayload = null,
    string? PaymentUrl = null,
    string? VirtualAccountNumber = null,
    DateTime? ExpiresAt = null);

public interface IPaymentProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    /// <summary>Cara bayar yang didukung penyedia ini.</summary>
    IReadOnlyList<PaymentChannel> SupportedChannels { get; }
    Task<ChargeResult> CreateChargeAsync(Models.Payment payment, CancellationToken ct);
    /// <summary>Tanya status terkini ke penyedia. Null berarti penyedia tidak mendukung pengecekan.</summary>
    Task<PaymentState?> QueryStatusAsync(Models.Payment payment, CancellationToken ct);
}

// ============================================================
// Manual - transfer bank, diverifikasi petugas
// ============================================================
public class ManualPaymentProvider : IPaymentProvider
{
    private readonly PaymentConfig _cfg;
    public ManualPaymentProvider(PaymentConfig cfg) => _cfg = cfg;

    public string Name => "Manual";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_cfg.Manual?.BankName) &&
        !string.IsNullOrWhiteSpace(_cfg.Manual?.AccountNumber);

    public IReadOnlyList<PaymentChannel> SupportedChannels =>
        new[] { PaymentChannel.BankTransfer, PaymentChannel.Cash };

    public Task<ChargeResult> CreateChargeAsync(Models.Payment payment, CancellationToken ct) =>
        Task.FromResult(new ChargeResult("Manual", payment.Channel,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_cfg.ExpiryMinutes)));

    public Task<PaymentState?> QueryStatusAsync(Models.Payment payment, CancellationToken ct) =>
        Task.FromResult<PaymentState?>(null); // hanya bisa diverifikasi manusia
}

// ============================================================
// QRIS statis milik merchant, dijadikan dinamis
// ============================================================
public class QrisPaymentProvider : IPaymentProvider
{
    private readonly PaymentConfig _cfg;
    private readonly ILogger<QrisPaymentProvider> _log;

    public QrisPaymentProvider(PaymentConfig cfg, ILogger<QrisPaymentProvider> log)
    { _cfg = cfg; _log = log; }

    public string Name => "Qris";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_cfg.Qris?.StaticPayload);

    public IReadOnlyList<PaymentChannel> SupportedChannels => new[] { PaymentChannel.Qris };

    public Task<ChargeResult> CreateChargeAsync(Models.Payment payment, CancellationToken ct)
    {
        var dynamicPayload = QrisPayload.WithAmount(_cfg.Qris!.StaticPayload, payment.Total);
        _log.LogInformation("[Payment] QRIS dinamis dibuat untuk {Invoice}", payment.InvoiceNumber);

        return Task.FromResult(new ChargeResult("Qris", PaymentChannel.Qris,
            QrPayload: dynamicPayload,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_cfg.ExpiryMinutes)));
    }

    // QRIS statis tidak punya API status; dana masuk diperiksa lewat mutasi rekening.
    public Task<PaymentState?> QueryStatusAsync(Models.Payment payment, CancellationToken ct) =>
        Task.FromResult<PaymentState?>(null);
}

// ============================================================
// Midtrans Core API
// ============================================================
public class MidtransPaymentProvider : IPaymentProvider
{
    private readonly PaymentConfig _cfg;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<MidtransPaymentProvider> _log;

    public MidtransPaymentProvider(PaymentConfig cfg, IHttpClientFactory http, ILogger<MidtransPaymentProvider> log)
    { _cfg = cfg; _http = http; _log = log; }

    public string Name => "Midtrans";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_cfg.Midtrans?.ServerKey);

    public IReadOnlyList<PaymentChannel> SupportedChannels =>
        new[] { PaymentChannel.Qris, PaymentChannel.VirtualAccount, PaymentChannel.EWallet, PaymentChannel.Card };

    private string BaseUrl => _cfg.Midtrans!.IsProduction
        ? "https://api.midtrans.com"
        : "https://api.sandbox.midtrans.com";

    private HttpClient Client()
    {
        var client = _http.CreateClient("PaymentClient");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(_cfg.Midtrans!.ServerKey + ":"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        return client;
    }

    public async Task<ChargeResult> CreateChargeAsync(Models.Payment payment, CancellationToken ct)
    {
        var client = Client();
        var gross = (long)decimal.Round(payment.Total, 0);

        object body = payment.Channel switch
        {
            PaymentChannel.Qris => new
            {
                payment_type = "qris",
                transaction_details = new { order_id = payment.InvoiceNumber, gross_amount = gross },
                qris = new { acquirer = "gopay" }
            },
            PaymentChannel.VirtualAccount => new
            {
                payment_type = "bank_transfer",
                transaction_details = new { order_id = payment.InvoiceNumber, gross_amount = gross },
                bank_transfer = new { bank = "bca" }
            },
            _ => new
            {
                payment_type = "gopay",
                transaction_details = new { order_id = payment.InvoiceNumber, gross_amount = gross }
            }
        };

        var res = await client.PostAsJsonAsync($"{BaseUrl}/v2/charge", body, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Midtrans menolak permintaan ({(int)res.StatusCode}): {text}");

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        string? qr = null, url = null, va = null;

        if (root.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
        {
            foreach (var action in actions.EnumerateArray())
            {
                var name = action.GetProperty("name").GetString();
                var link = action.TryGetProperty("url", out var u) ? u.GetString() : null;
                if (name is "generate-qr-code") qr = link;
                if (name is "deeplink-redirect") url = link;
            }
        }

        if (root.TryGetProperty("va_numbers", out var vas) && vas.ValueKind == JsonValueKind.Array && vas.GetArrayLength() > 0)
            va = vas[0].TryGetProperty("va_number", out var v) ? v.GetString() : null;

        var expiry = root.TryGetProperty("expiry_time", out var e) && DateTime.TryParse(e.GetString(), out var parsed)
            ? parsed.ToUniversalTime()
            : DateTime.UtcNow.AddMinutes(_cfg.ExpiryMinutes);

        _log.LogInformation("[Payment] Midtrans charge {Invoice} dibuat", payment.InvoiceNumber);

        return new ChargeResult("Midtrans", payment.Channel,
            ExternalId: root.TryGetProperty("transaction_id", out var tid) ? tid.GetString() : null,
            QrPayload: qr,          // Midtrans memberi URL gambar QR, bukan payload mentah
            PaymentUrl: url,
            VirtualAccountNumber: va,
            ExpiresAt: expiry);
    }

    public async Task<PaymentState?> QueryStatusAsync(Models.Payment payment, CancellationToken ct)
    {
        var client = Client();
        var res = await client.GetAsync($"{BaseUrl}/v2/{payment.InvoiceNumber}/status", ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(text);
        var status = doc.RootElement.TryGetProperty("transaction_status", out var s) ? s.GetString() : null;

        return status switch
        {
            "settlement" or "capture" => PaymentState.Paid,
            "pending" => PaymentState.Pending,
            "expire" => PaymentState.Expired,
            "deny" or "cancel" or "failure" => PaymentState.Failed,
            "refund" or "partial_refund" => PaymentState.Refunded,
            _ => null
        };
    }
}

// ============================================================
// Xendit
// ============================================================
public class XenditPaymentProvider : IPaymentProvider
{
    private readonly PaymentConfig _cfg;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<XenditPaymentProvider> _log;

    public XenditPaymentProvider(PaymentConfig cfg, IHttpClientFactory http, ILogger<XenditPaymentProvider> log)
    { _cfg = cfg; _http = http; _log = log; }

    public string Name => "Xendit";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_cfg.Xendit?.SecretKey);

    public IReadOnlyList<PaymentChannel> SupportedChannels =>
        new[] { PaymentChannel.Qris, PaymentChannel.VirtualAccount, PaymentChannel.EWallet };

    private HttpClient Client()
    {
        var client = _http.CreateClient("PaymentClient");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(_cfg.Xendit!.SecretKey + ":"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        return client;
    }

    public async Task<ChargeResult> CreateChargeAsync(Models.Payment payment, CancellationToken ct)
    {
        var client = Client();

        if (payment.Channel == PaymentChannel.Qris)
        {
            var body = new
            {
                reference_id = payment.InvoiceNumber,
                type = "DYNAMIC",
                currency = "IDR",
                amount = decimal.Round(payment.Total, 0),
                expires_at = DateTime.UtcNow.AddMinutes(_cfg.ExpiryMinutes).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            var res = await client.PostAsJsonAsync("https://api.xendit.co/qr_codes", body, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Xendit menolak permintaan ({(int)res.StatusCode}): {text}");

            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            _log.LogInformation("[Payment] Xendit QR {Invoice} dibuat", payment.InvoiceNumber);

            return new ChargeResult("Xendit", PaymentChannel.Qris,
                ExternalId: root.TryGetProperty("id", out var id) ? id.GetString() : null,
                QrPayload: root.TryGetProperty("qr_string", out var qs) ? qs.GetString() : null,
                ExpiresAt: DateTime.UtcNow.AddMinutes(_cfg.ExpiryMinutes));
        }

        // Kanal lain memakai invoice Xendit yang membuka halaman bayar
        var invoiceBody = new
        {
            external_id = payment.InvoiceNumber,
            amount = decimal.Round(payment.Total, 0),
            description = payment.Description,
            invoice_duration = _cfg.ExpiryMinutes * 60
        };

        var invoiceRes = await client.PostAsJsonAsync("https://api.xendit.co/v2/invoices", invoiceBody, ct);
        var invoiceText = await invoiceRes.Content.ReadAsStringAsync(ct);
        if (!invoiceRes.IsSuccessStatusCode)
            throw new InvalidOperationException($"Xendit menolak permintaan ({(int)invoiceRes.StatusCode}): {invoiceText}");

        using var invoiceDoc = JsonDocument.Parse(invoiceText);
        var invoiceRoot = invoiceDoc.RootElement;

        return new ChargeResult("Xendit", payment.Channel,
            ExternalId: invoiceRoot.TryGetProperty("id", out var iid) ? iid.GetString() : null,
            PaymentUrl: invoiceRoot.TryGetProperty("invoice_url", out var iu) ? iu.GetString() : null,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_cfg.ExpiryMinutes));
    }

    public async Task<PaymentState?> QueryStatusAsync(Models.Payment payment, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(payment.ExternalId)) return null;
        var client = Client();

        var url = payment.Channel == PaymentChannel.Qris
            ? $"https://api.xendit.co/qr_codes/{payment.ExternalId}"
            : $"https://api.xendit.co/v2/invoices/{payment.ExternalId}";

        var res = await client.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;

        return status switch
        {
            "ACTIVE" or "PENDING" => PaymentState.Pending,
            "COMPLETED" or "PAID" or "SETTLED" => PaymentState.Paid,
            "EXPIRED" => PaymentState.Expired,
            "INACTIVE" or "FAILED" => PaymentState.Failed,
            _ => null
        };
    }
}
