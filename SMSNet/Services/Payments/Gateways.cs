using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SMSNet.Models;

namespace SMSNet.Services.Payments;

/// <summary>
/// Shared behaviour for the hosted providers.
/// <para>
/// Each provider has a real integration point, marked <c>LIVE CALL</c>, that is
/// only reached when the gateway is out of sandbox mode and holds credentials.
/// In sandbox the charge is simulated locally: the whole checkout flow —
/// reference generation, status transitions, reconciliation — is exercised
/// without a live merchant account, which is the state a school installs in.
/// </para>
/// </summary>
public abstract class HostedGatewayBase : IPaymentGateway
{
    protected readonly IHttpClientFactory HttpFactory;
    protected readonly ILogger Logger;

    protected HostedGatewayBase(IHttpClientFactory httpFactory, ILogger logger)
    {
        HttpFactory = httpFactory;
        Logger = logger;
    }

    public abstract string Key { get; }
    public abstract PaymentChannelKind Channel { get; }
    public virtual bool RequiresCredentials => true;

    public async Task<ChargeResult> CreateChargeAsync(
        ChargeRequest request,
        PaymentGatewayConfig config,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            return ChargeResult.Fail("Nominal pembayaran harus lebih besar dari nol.");
        }

        if (config.SandboxMode || !HasCredentials(config))
        {
            return Simulate(request, config);
        }

        try
        {
            return await CreateLiveChargeAsync(request, config, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "{Gateway} charge failed for {Reference}", Key, request.Reference);
            return ChargeResult.Fail($"Gagal menghubungi {config.DisplayName}: {ex.Message}");
        }
    }

    protected virtual bool HasCredentials(PaymentGatewayConfig config) =>
        !string.IsNullOrWhiteSpace(config.ApiKey) || !string.IsNullOrWhiteSpace(config.SecretKey);

    /// <summary>The real provider call. Only invoked outside sandbox mode.</summary>
    protected abstract Task<ChargeResult> CreateLiveChargeAsync(
        ChargeRequest request,
        PaymentGatewayConfig config,
        CancellationToken cancellationToken);

    /// <summary>Local stand-in that mirrors what the provider would return.</summary>
    protected abstract ChargeResult Simulate(ChargeRequest request, PaymentGatewayConfig config);

    protected static string SandboxToken(string reference) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reference)))[..16].ToLowerInvariant();
}

// --- Midtrans ---------------------------------------------------------------

/// <summary>Midtrans Snap — the most common hosted checkout in Indonesian schools.</summary>
public sealed class MidtransGateway : HostedGatewayBase
{
    public MidtransGateway(IHttpClientFactory httpFactory, ILogger<MidtransGateway> logger)
        : base(httpFactory, logger) { }

    public override string Key => "midtrans";
    public override PaymentChannelKind Channel => PaymentChannelKind.Redirect;

    protected override async Task<ChargeResult> CreateLiveChargeAsync(
        ChargeRequest request, PaymentGatewayConfig config, CancellationToken cancellationToken)
    {
        // LIVE CALL — Snap transaction endpoint.
        var baseUrl = config.SandboxMode
            ? "https://app.sandbox.midtrans.com/snap/v1/transactions"
            : "https://app.midtrans.com/snap/v1/transactions";

        var client = HttpFactory.CreateClient("payments");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.SecretKey}:"));
        client.DefaultRequestHeaders.Authorization = new("Basic", auth);

        var payload = new
        {
            transaction_details = new
            {
                order_id = request.Reference,
                gross_amount = (long)request.Amount
            },
            customer_details = new
            {
                first_name = request.StudentName,
                email = request.PayerEmail,
                phone = request.PayerPhone
            },
            item_details = new[]
            {
                new { id = request.Category, price = (long)request.Amount, quantity = 1, name = request.Category }
            }
        };

        using var response = await client.PostAsJsonAsync(baseUrl, payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ChargeResult.Fail($"Midtrans menolak permintaan ({(int)response.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var token = doc.RootElement.TryGetProperty("token", out var t) ? t.GetString() : null;
        var redirect = doc.RootElement.TryGetProperty("redirect_url", out var r) ? r.GetString() : null;

        return ChargeResult.Ok(PaymentStatus.Pending, token, redirect,
            "Selesaikan pembayaran pada halaman Midtrans yang terbuka.");
    }

    protected override ChargeResult Simulate(ChargeRequest request, PaymentGatewayConfig config) =>
        ChargeResult.Ok(
            PaymentStatus.Pending,
            $"snap-sandbox-{SandboxToken(request.Reference)}",
            $"/payments/simulate/{request.Reference}",
            "Mode sandbox: tidak ada panggilan ke Midtrans. Gunakan tombol konfirmasi manual untuk menandai lunas.");
}

// --- Xendit -----------------------------------------------------------------

/// <summary>Xendit invoices — hosted page covering VA, e-wallet, and retail outlets.</summary>
public sealed class XenditGateway : HostedGatewayBase
{
    public XenditGateway(IHttpClientFactory httpFactory, ILogger<XenditGateway> logger)
        : base(httpFactory, logger) { }

    public override string Key => "xendit";
    public override PaymentChannelKind Channel => PaymentChannelKind.Redirect;

    protected override async Task<ChargeResult> CreateLiveChargeAsync(
        ChargeRequest request, PaymentGatewayConfig config, CancellationToken cancellationToken)
    {
        // LIVE CALL — Invoice endpoint.
        var client = HttpFactory.CreateClient("payments");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.SecretKey}:"));
        client.DefaultRequestHeaders.Authorization = new("Basic", auth);

        var payload = new
        {
            external_id = request.Reference,
            amount = request.Amount,
            description = $"{request.Category} — {request.StudentName}",
            payer_email = request.PayerEmail,
            currency = "IDR"
        };

        using var response = await client.PostAsJsonAsync(
            "https://api.xendit.co/v2/invoices", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ChargeResult.Fail($"Xendit menolak permintaan ({(int)response.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var id = doc.RootElement.TryGetProperty("id", out var i) ? i.GetString() : null;
        var url = doc.RootElement.TryGetProperty("invoice_url", out var u) ? u.GetString() : null;

        return ChargeResult.Ok(PaymentStatus.Pending, id, url,
            "Selesaikan pembayaran pada halaman invoice Xendit.");
    }

    protected override ChargeResult Simulate(ChargeRequest request, PaymentGatewayConfig config) =>
        ChargeResult.Ok(
            PaymentStatus.Pending,
            $"inv-sandbox-{SandboxToken(request.Reference)}",
            $"/payments/simulate/{request.Reference}",
            "Mode sandbox: tidak ada panggilan ke Xendit. Gunakan konfirmasi manual untuk menandai lunas.");
}

// --- Stripe -----------------------------------------------------------------

/// <summary>Stripe Checkout — for schools billing international families.</summary>
public sealed class StripeGateway : HostedGatewayBase
{
    public StripeGateway(IHttpClientFactory httpFactory, ILogger<StripeGateway> logger)
        : base(httpFactory, logger) { }

    public override string Key => "stripe";
    public override PaymentChannelKind Channel => PaymentChannelKind.Redirect;

    protected override async Task<ChargeResult> CreateLiveChargeAsync(
        ChargeRequest request, PaymentGatewayConfig config, CancellationToken cancellationToken)
    {
        // LIVE CALL — Checkout Session. Stripe takes form encoding, not JSON.
        var client = HttpFactory.CreateClient("payments");
        client.DefaultRequestHeaders.Authorization = new("Bearer", config.SecretKey);

        var form = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["client_reference_id"] = request.Reference,
            ["success_url"] = "https://example.invalid/payments/success",
            ["cancel_url"] = "https://example.invalid/payments/cancel",
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = "idr",
            ["line_items[0][price_data][unit_amount]"] = ((long)request.Amount).ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = $"{request.Category} — {request.StudentName}"
        };

        using var response = await client.PostAsync(
            "https://api.stripe.com/v1/checkout/sessions", new FormUrlEncodedContent(form), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ChargeResult.Fail($"Stripe menolak permintaan ({(int)response.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var id = doc.RootElement.TryGetProperty("id", out var i) ? i.GetString() : null;
        var url = doc.RootElement.TryGetProperty("url", out var u) ? u.GetString() : null;

        return ChargeResult.Ok(PaymentStatus.Pending, id, url, "Selesaikan pembayaran pada halaman Stripe.");
    }

    protected override ChargeResult Simulate(ChargeRequest request, PaymentGatewayConfig config) =>
        ChargeResult.Ok(
            PaymentStatus.Pending,
            $"cs_test_{SandboxToken(request.Reference)}",
            $"/payments/simulate/{request.Reference}",
            "Mode sandbox: tidak ada panggilan ke Stripe. Gunakan konfirmasi manual untuk menandai lunas.");
}

// --- QRIS -------------------------------------------------------------------

/// <summary>
/// Static QRIS. The school's own merchant code is shown as a QR; the payer scans
/// it with any Indonesian banking or e-wallet app. No API call is involved, so
/// this works with no credentials at all — only a merchant string.
/// </summary>
public sealed class QrisGateway : IPaymentGateway
{
    public string Key => "qris";
    public PaymentChannelKind Channel => PaymentChannelKind.QrCode;
    public bool RequiresCredentials => false;

    public Task<ChargeResult> CreateChargeAsync(
        ChargeRequest request, PaymentGatewayConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.AccountDetail))
        {
            return Task.FromResult(ChargeResult.Fail(
                "Kode merchant QRIS belum diisi pada pengaturan metode pembayaran."));
        }

        var instructions = string.IsNullOrWhiteSpace(config.Instructions)
            ? "Pindai kode QR dengan aplikasi mobile banking atau e-wallet, lalu unggah bukti pembayaran."
            : config.Instructions;

        return Task.FromResult(ChargeResult.Ok(
            PaymentStatus.AwaitingConfirmation,
            null,
            config.AccountDetail,
            instructions));
    }
}

// --- Manual transfer --------------------------------------------------------

/// <summary>
/// Bank transfer confirmed by the school. This is the fallback every school can
/// use on day one, before any provider account exists.
/// </summary>
public sealed class ManualTransferGateway : IPaymentGateway
{
    public string Key => "manual";
    public PaymentChannelKind Channel => PaymentChannelKind.ManualTransfer;
    public bool RequiresCredentials => false;

    public Task<ChargeResult> CreateChargeAsync(
        ChargeRequest request, PaymentGatewayConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.AccountDetail))
        {
            return Task.FromResult(ChargeResult.Fail(
                "Nomor rekening sekolah belum diisi pada pengaturan metode pembayaran."));
        }

        var instructions = string.IsNullOrWhiteSpace(config.Instructions)
            ? $"Transfer ke {config.AccountDetail} sejumlah {request.Amount:N0}, " +
              $"cantumkan kode {request.Reference} pada berita transfer."
            : config.Instructions;

        return Task.FromResult(ChargeResult.Ok(
            PaymentStatus.AwaitingConfirmation,
            null,
            config.AccountDetail,
            instructions));
    }
}
