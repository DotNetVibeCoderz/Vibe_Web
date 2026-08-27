using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lapak.Models;
using Lapak.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Lapak.Services.Payment;

/// <summary>
/// Stripe Checkout Sessions, called over the raw REST API so the project keeps a
/// single HTTP-based shape for every gateway instead of pulling in an SDK.
/// Webhooks are authenticated with the timestamped HMAC in Stripe-Signature.
/// </summary>
public class StripePaymentProvider : IPaymentProvider
{
    private readonly StripeConfig _config;
    private readonly string _publicBaseUrl;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StripePaymentProvider> _logger;

    /// <summary>Stripe rejects a signature older than this to blunt replay attacks.</summary>
    private static readonly TimeSpan SignatureTolerance = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Currencies Stripe treats as having no minor unit: the amount is sent whole
    /// rather than multiplied by 100. IDR is one of them.
    /// </summary>
    private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga",
        "pyg", "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf", "idr"
    };

    public StripePaymentProvider(
        IOptions<PaymentGatewayConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<StripePaymentProvider> logger)
    {
        _config = config.Value.Stripe;
        _publicBaseUrl = config.Value.PublicBaseUrl.TrimEnd('/');
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "Stripe";
    public string DisplayName => "Stripe — kartu kredit & debit";
    public bool IsConfigured => _config.Enabled && !string.IsNullOrWhiteSpace(_config.SecretKey);

    public IReadOnlyList<PaymentMethodOption> SupportedMethods { get; } = new[]
    {
        new PaymentMethodOption("card", "Kartu kredit / debit", "Kartu"),
        new PaymentMethodOption("alipay", "Alipay", "E-wallet"),
        new PaymentMethodOption("wechat_pay", "WeChat Pay", "E-wallet")
    };

    public async Task<PaymentResponse> CreatePaymentAsync(Order order, PaymentRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return PaymentResponse.Failed(Name, "Stripe belum dikonfigurasi. Isi SecretKey di appsettings.json.");

        try
        {
            var form = BuildCheckoutSessionForm(order, request);

            var client = _httpClientFactory.CreateClient("PaymentClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.SecretKey);

            using var content = new FormUrlEncodedContent(form);
            var response = await client.PostAsync($"{_config.BaseUrl.TrimEnd('/')}/v1/checkout/sessions", content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Stripe checkout session failed ({Status}): {Body}", response.StatusCode, body);
                return PaymentResponse.Failed(Name, $"Stripe menolak transaksi: {ExtractError(body)}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            return new PaymentResponse
            {
                Success = true,
                Gateway = Name,
                TransactionId = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                PaymentUrl = root.TryGetProperty("url", out var url) ? url.GetString() ?? "" : "",
                Instruction = "Kamu akan diarahkan ke Stripe Checkout untuk memasukkan detail kartu.",
                State = MapSessionState(
                    root.TryGetProperty("payment_status", out var ps) ? ps.GetString() ?? "unpaid" : "unpaid")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe payment creation failed for order {OrderNumber}", order.OrderNumber);
            return PaymentResponse.Failed(Name, ex.Message);
        }
    }

    public Task<PaymentCallbackResult> HandleCallbackAsync(PaymentCallbackContext context, CancellationToken ct = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_config.WebhookSecret))
            {
                var signatureHeader = context.Header("Stripe-Signature");
                if (!VerifySignature(context.RawBody, signatureHeader, _config.WebhookSecret, out var reason))
                {
                    _logger.LogWarning("Stripe webhook rejected: {Reason}", reason);
                    return Task.FromResult(PaymentCallbackResult.Rejected(reason));
                }
            }

            using var doc = JsonDocument.Parse(context.RawBody);
            var root = doc.RootElement;

            var eventType = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
            if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("object", out var obj))
                return Task.FromResult(PaymentCallbackResult.Failed("Payload webhook tidak berisi data.object."));

            string Read(JsonElement el, string name) =>
                el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";

            // client_reference_id carries our own order number through Stripe.
            var orderNumber = Read(obj, "client_reference_id");
            if (string.IsNullOrEmpty(orderNumber))
                orderNumber = Read(obj, "metadata_order_number");

            var state = eventType switch
            {
                "checkout.session.completed" or "checkout.session.async_payment_succeeded" =>
                    MapSessionState(Read(obj, "payment_status")),
                "checkout.session.async_payment_failed" => PaymentState.Failed,
                "checkout.session.expired" => PaymentState.Expired,
                "charge.refunded" => PaymentState.Refunded,
                _ => PaymentState.Pending
            };

            return Task.FromResult(new PaymentCallbackResult
            {
                Success = true,
                OrderNumber = orderNumber,
                TransactionId = Read(obj, "id"),
                State = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook processing failed");
            return Task.FromResult(PaymentCallbackResult.Failed(ex.Message));
        }
    }

    private List<KeyValuePair<string, string>> BuildCheckoutSessionForm(Order order, PaymentRequest request)
    {
        var currency = string.IsNullOrWhiteSpace(_config.Currency) ? "idr" : _config.Currency.ToLowerInvariant();
        var method = SupportedMethods.Any(m => m.Code == request.PaymentMethod) ? request.PaymentMethod : "card";

        var form = new List<KeyValuePair<string, string>>
        {
            new("mode", "payment"),
            new("client_reference_id", order.OrderNumber),
            new("payment_method_types[0]", method),
            new("success_url", $"{_publicBaseUrl}/account/orders/{order.OrderNumber}?payment=success"),
            new("cancel_url", $"{_publicBaseUrl}/account/orders/{order.OrderNumber}?payment=cancelled"),
            new("metadata[order_number]", order.OrderNumber),
            new("metadata[order_id]", order.Id.ToString())
        };

        if (!string.IsNullOrWhiteSpace(order.User?.Email))
            form.Add(new KeyValuePair<string, string>("customer_email", order.User!.Email!));

        if (order.Discount > 0)
        {
            // Stripe line items cannot be negative, so a discounted order is billed
            // as one summary line that already nets out the voucher. The itemised
            // breakdown still lives on the order page.
            AddLineItem(form, 0, currency, $"Pesanan #{order.OrderNumber} (setelah diskon voucher)", order.GrandTotal, 1);
            return form;
        }

        var line = 0;
        foreach (var item in order.OrderItems)
            AddLineItem(form, line++, currency, item.Product?.Name ?? "Produk", item.Price, item.Quantity);

        if (order.ShippingCost > 0)
            AddLineItem(form, line, currency, $"Ongkos kirim ({order.ShippingCourier} {order.ShippingService})".Trim(), order.ShippingCost, 1);

        return form;
    }

    private void AddLineItem(List<KeyValuePair<string, string>> form, int index, string currency, string name, decimal amount, int quantity)
    {
        form.Add(new KeyValuePair<string, string>($"line_items[{index}][price_data][currency]", currency));
        form.Add(new KeyValuePair<string, string>($"line_items[{index}][price_data][product_data][name]", Truncate(name, 250)));
        form.Add(new KeyValuePair<string, string>($"line_items[{index}][price_data][unit_amount]", ToMinorUnits(amount, currency)));
        form.Add(new KeyValuePair<string, string>($"line_items[{index}][quantity]", quantity.ToString(CultureInfo.InvariantCulture)));
    }

    private static string ToMinorUnits(decimal amount, string currency)
    {
        var value = ZeroDecimalCurrencies.Contains(currency)
            ? (long)Math.Round(amount, MidpointRounding.AwayFromZero)
            : (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Stripe-Signature looks like "t=1699999999,v1=abc...". The signed payload is
    /// "{timestamp}.{body}" hashed with HMAC-SHA256 under the webhook secret.
    /// </summary>
    internal static bool VerifySignature(string payload, string? signatureHeader, string secret, out string reason)
    {
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            reason = "Header Stripe-Signature tidak ada.";
            return false;
        }

        string? timestamp = null;
        var candidates = new List<string>();

        foreach (var part in signatureHeader.Split(','))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2) continue;

            var key = pair[0].Trim();
            var value = pair[1].Trim();

            if (key == "t") timestamp = value;
            else if (key == "v1") candidates.Add(value);
        }

        if (timestamp is null || candidates.Count == 0)
        {
            reason = "Format Stripe-Signature tidak dikenali.";
            return false;
        }

        if (!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixSeconds))
        {
            reason = "Timestamp Stripe-Signature tidak valid.";
            return false;
        }

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        if (age > SignatureTolerance || age < -SignatureTolerance)
        {
            reason = "Stripe-Signature kedaluwarsa.";
            return false;
        }

        var signedPayload = Encoding.UTF8.GetBytes($"{timestamp}.{payload}");
        var expected = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signedPayload)).ToLowerInvariant();

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        foreach (var candidate in candidates)
        {
            var candidateBytes = Encoding.UTF8.GetBytes(candidate.ToLowerInvariant());
            if (candidateBytes.Length == expectedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(candidateBytes, expectedBytes))
                return true;
        }

        reason = "Signature tidak cocok.";
        return false;
    }

    private static PaymentState MapSessionState(string paymentStatus) => paymentStatus switch
    {
        "paid" => PaymentState.Paid,
        "no_payment_required" => PaymentState.Paid,
        _ => PaymentState.Pending
    };

    private static string ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
                return msg.GetString() ?? body;
        }
        catch { /* fall through to the raw body */ }
        return Truncate(body, 200);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
