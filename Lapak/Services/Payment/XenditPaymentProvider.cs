using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lapak.Models;
using Lapak.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Lapak.Services.Payment;

/// <summary>
/// Xendit Invoice API. Every method funnels through one hosted invoice page, so
/// the buyer is redirected rather than shown a VA number inline. Callbacks are
/// authenticated with the static x-callback-token header.
/// </summary>
public class XenditPaymentProvider : IPaymentProvider
{
    private readonly XenditConfig _config;
    private readonly string _publicBaseUrl;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<XenditPaymentProvider> _logger;

    public XenditPaymentProvider(
        IOptions<PaymentGatewayConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<XenditPaymentProvider> logger)
    {
        _config = config.Value.Xendit;
        _publicBaseUrl = config.Value.PublicBaseUrl.TrimEnd('/');
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "Xendit";
    public string DisplayName => "Xendit — halaman pembayaran";
    public bool IsConfigured => _config.Enabled && !string.IsNullOrWhiteSpace(_config.ApiKey);

    public IReadOnlyList<PaymentMethodOption> SupportedMethods { get; } = new[]
    {
        new PaymentMethodOption("BCA", "BCA Virtual Account", "Transfer bank"),
        new PaymentMethodOption("BNI", "BNI Virtual Account", "Transfer bank"),
        new PaymentMethodOption("BRI", "BRI Virtual Account", "Transfer bank"),
        new PaymentMethodOption("MANDIRI", "Mandiri Virtual Account", "Transfer bank"),
        new PaymentMethodOption("OVO", "OVO", "E-wallet"),
        new PaymentMethodOption("DANA", "DANA", "E-wallet"),
        new PaymentMethodOption("SHOPEEPAY", "ShopeePay", "E-wallet"),
        new PaymentMethodOption("QRIS", "QRIS", "E-wallet")
    };

    public async Task<PaymentResponse> CreatePaymentAsync(Order order, PaymentRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return PaymentResponse.Failed(Name, "Xendit belum dikonfigurasi. Isi ApiKey di appsettings.json.");

        try
        {
            // Restrict the invoice to the channel the buyer picked; fall back to
            // every supported channel when the code is unrecognised.
            var chosen = SupportedMethods.FirstOrDefault(m =>
                m.Code.Equals(request.PaymentMethod, StringComparison.OrdinalIgnoreCase));

            var payload = new
            {
                external_id = order.OrderNumber,
                amount = (long)order.GrandTotal,
                payer_email = string.IsNullOrWhiteSpace(order.User?.Email) ? "customer@lapak.com" : order.User!.Email,
                description = $"Pembayaran pesanan #{order.OrderNumber} di Lapak",
                currency = "IDR",
                invoice_duration = 86400,
                payment_methods = chosen is null
                    ? SupportedMethods.Select(m => m.Code).ToArray()
                    : new[] { chosen.Code },
                success_redirect_url = $"{_publicBaseUrl}/account/orders/{order.OrderNumber}",
                failure_redirect_url = $"{_publicBaseUrl}/account/orders/{order.OrderNumber}",
                items = order.OrderItems.Select(i => new
                {
                    name = i.Product?.Name ?? "Produk",
                    quantity = i.Quantity,
                    price = (long)i.Price
                }).ToArray()
            };

            var client = _httpClientFactory.CreateClient("PaymentClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(_config.ApiKey + ":")));

            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_config.BaseUrl.TrimEnd('/')}/v2/invoices", content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Xendit invoice failed ({Status}): {Body}", response.StatusCode, body);
                return PaymentResponse.Failed(Name, $"Xendit menolak transaksi: {ExtractError(body)}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            return new PaymentResponse
            {
                Success = true,
                Gateway = Name,
                TransactionId = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                PaymentUrl = root.TryGetProperty("invoice_url", out var url) ? url.GetString() ?? "" : "",
                Instruction = "Kamu akan diarahkan ke halaman pembayaran Xendit untuk menyelesaikan transaksi.",
                State = MapState(root.TryGetProperty("status", out var st) ? st.GetString() ?? "PENDING" : "PENDING")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xendit payment creation failed for order {OrderNumber}", order.OrderNumber);
            return PaymentResponse.Failed(Name, ex.Message);
        }
    }

    public Task<PaymentCallbackResult> HandleCallbackAsync(PaymentCallbackContext context, CancellationToken ct = default)
    {
        try
        {
            // Xendit authenticates webhooks with a static token echoed in this header.
            if (!string.IsNullOrWhiteSpace(_config.CallbackToken))
            {
                var token = context.Header("x-callback-token");
                if (string.IsNullOrEmpty(token) || !CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(token),
                        Encoding.UTF8.GetBytes(_config.CallbackToken)))
                {
                    _logger.LogWarning("Xendit callback rejected: invalid x-callback-token");
                    return Task.FromResult(PaymentCallbackResult.Rejected("Callback token tidak valid."));
                }
            }

            using var doc = JsonDocument.Parse(context.RawBody);
            var root = doc.RootElement;

            string Read(string name) => root.TryGetProperty(name, out var el) ? el.GetString() ?? "" : "";

            return Task.FromResult(new PaymentCallbackResult
            {
                Success = true,
                OrderNumber = Read("external_id"),
                TransactionId = Read("id"),
                State = MapState(Read("status"))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xendit callback processing failed");
            return Task.FromResult(PaymentCallbackResult.Failed(ex.Message));
        }
    }

    private static PaymentState MapState(string status) => status.ToUpperInvariant() switch
    {
        "PAID" or "SETTLED" => PaymentState.Paid,
        "EXPIRED" => PaymentState.Expired,
        "FAILED" => PaymentState.Failed,
        _ => PaymentState.Pending
    };

    private static string ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString() ?? body;
        }
        catch { /* fall through to the raw body */ }
        return body.Length <= 200 ? body : body[..200];
    }
}
