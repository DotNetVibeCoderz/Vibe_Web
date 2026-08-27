using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lapak.Models;
using Lapak.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Lapak.Services.Payment;

/// <summary>
/// Midtrans Core API. Bank transfer returns a virtual account number the buyer
/// pays into; e-wallets return a deeplink. Callbacks are authenticated with the
/// SHA-512 signature Midtrans puts in the callback body.
/// </summary>
public class MidtransPaymentProvider : IPaymentProvider
{
    private readonly MidtransConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MidtransPaymentProvider> _logger;

    public MidtransPaymentProvider(
        IOptions<PaymentGatewayConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<MidtransPaymentProvider> logger)
    {
        _config = config.Value.Midtrans;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "Midtrans";
    public string DisplayName => "Midtrans — VA & e-wallet";
    public bool IsConfigured => _config.Enabled && !string.IsNullOrWhiteSpace(_config.ServerKey);

    public IReadOnlyList<PaymentMethodOption> SupportedMethods { get; } = new[]
    {
        new PaymentMethodOption("bank_transfer:bca", "BCA Virtual Account", "Transfer bank"),
        new PaymentMethodOption("bank_transfer:bni", "BNI Virtual Account", "Transfer bank"),
        new PaymentMethodOption("bank_transfer:bri", "BRI Virtual Account", "Transfer bank"),
        new PaymentMethodOption("echannel:mandiri", "Mandiri Bill Payment", "Transfer bank"),
        new PaymentMethodOption("gopay:gopay", "GoPay", "E-wallet"),
        new PaymentMethodOption("qris:qris", "QRIS", "E-wallet")
    };

    private string ApiBaseUrl => _config.IsProduction
        ? "https://api.midtrans.com/v2"
        : "https://api.sandbox.midtrans.com/v2";

    public async Task<PaymentResponse> CreatePaymentAsync(Order order, PaymentRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return PaymentResponse.Failed(Name, "Midtrans belum dikonfigurasi. Isi ServerKey di appsettings.json.");

        try
        {
            var payload = BuildChargePayload(order, request);
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var client = _httpClientFactory.CreateClient("PaymentClient");
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(_config.ServerKey + ":"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{ApiBaseUrl}/charge", content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Midtrans charge failed ({Status}): {Body}", response.StatusCode, body);
                return PaymentResponse.Failed(Name, $"Midtrans menolak transaksi: {ExtractMidtransError(body)}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var transactionId = root.TryGetProperty("transaction_id", out var tid) ? tid.GetString() ?? "" : "";
            var status = root.TryGetProperty("transaction_status", out var ts) ? ts.GetString() ?? "pending" : "pending";

            var vaNumber = "";
            var instruction = "";

            if (root.TryGetProperty("va_numbers", out var vaNumbers) && vaNumbers.GetArrayLength() > 0)
            {
                var first = vaNumbers[0];
                vaNumber = first.TryGetProperty("va_number", out var v) ? v.GetString() ?? "" : "";
                var bank = first.TryGetProperty("bank", out var b) ? b.GetString()?.ToUpperInvariant() ?? "" : "";
                instruction = $"Transfer ke Virtual Account {bank} di atas sebelum batas waktu pembayaran.";
            }
            else if (root.TryGetProperty("bill_key", out var billKey))
            {
                var billerCode = root.TryGetProperty("biller_code", out var bc) ? bc.GetString() ?? "" : "";
                vaNumber = billKey.GetString() ?? "";
                instruction = $"Bayar via Mandiri Bill Payment. Kode perusahaan: {billerCode}.";
            }

            var paymentUrl = "";
            if (root.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
            {
                foreach (var action in actions.EnumerateArray())
                {
                    var actionName = action.TryGetProperty("name", out var an) ? an.GetString() : null;
                    if (actionName is "deeplink-redirect" or "generate-qr-code")
                    {
                        paymentUrl = action.TryGetProperty("url", out var au) ? au.GetString() ?? "" : "";
                        instruction = "Selesaikan pembayaran di aplikasi e-wallet kamu.";
                        break;
                    }
                }
            }

            return new PaymentResponse
            {
                Success = true,
                Gateway = Name,
                TransactionId = transactionId,
                VaNumber = vaNumber,
                PaymentUrl = paymentUrl,
                Instruction = instruction,
                State = MapState(status)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Midtrans payment creation failed for order {OrderNumber}", order.OrderNumber);
            return PaymentResponse.Failed(Name, ex.Message);
        }
    }

    public Task<PaymentCallbackResult> HandleCallbackAsync(PaymentCallbackContext context, CancellationToken ct = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(context.RawBody);
            var root = doc.RootElement;

            string Read(string name) => root.TryGetProperty(name, out var el) ? el.GetString() ?? "" : "";

            var orderId = Read("order_id");
            var statusCode = Read("status_code");
            var grossAmount = Read("gross_amount");
            var signature = Read("signature_key");
            var transactionStatus = Read("transaction_status");
            var fraudStatus = Read("fraud_status");

            // Midtrans signs order_id + status_code + gross_amount + server key with SHA-512.
            // Without this check anyone who knows an order number could mark it paid.
            var expected = ComputeSignature(orderId, statusCode, grossAmount, _config.ServerKey);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expected),
                    Encoding.UTF8.GetBytes(signature.ToLowerInvariant())))
            {
                _logger.LogWarning("Midtrans callback rejected: signature mismatch for order {OrderId}", orderId);
                return Task.FromResult(PaymentCallbackResult.Rejected("Signature tidak valid."));
            }

            // A captured card payment still needs a fraud review before it counts as paid.
            var state = MapState(transactionStatus);
            if (state == PaymentState.Paid && fraudStatus == "challenge")
                state = PaymentState.Pending;

            return Task.FromResult(new PaymentCallbackResult
            {
                Success = true,
                OrderNumber = orderId,
                TransactionId = Read("transaction_id"),
                State = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Midtrans callback processing failed");
            return Task.FromResult(PaymentCallbackResult.Failed(ex.Message));
        }
    }

    private object BuildChargePayload(Order order, PaymentRequest request)
    {
        var (paymentType, channel) = SplitMethod(request);

        var itemDetails = order.OrderItems.Select(i => new
        {
            id = i.ProductId.ToString(),
            price = (long)i.Price,
            quantity = i.Quantity,
            name = Truncate(i.Product?.Name ?? "Produk", 50)
        }).ToList();

        // Midtrans requires item_details to sum exactly to gross_amount.
        var itemTotal = order.OrderItems.Sum(i => (long)i.Price * i.Quantity);
        if (order.ShippingCost > 0)
        {
            itemDetails.Add(new { id = "shipping", price = (long)order.ShippingCost, quantity = 1, name = "Ongkos kirim" });
            itemTotal += (long)order.ShippingCost;
        }
        if (order.Discount > 0)
        {
            itemDetails.Add(new { id = "discount", price = -(long)order.Discount, quantity = 1, name = "Diskon voucher" });
            itemTotal -= (long)order.Discount;
        }

        return new
        {
            payment_type = paymentType,
            transaction_details = new
            {
                order_id = order.OrderNumber,
                gross_amount = itemTotal
            },
            customer_details = new
            {
                first_name = order.User?.FullName ?? "Pelanggan",
                email = order.User?.Email ?? "",
                phone = order.User?.PhoneNumber ?? ""
            },
            item_details = itemDetails,
            bank_transfer = paymentType == "bank_transfer" ? new { bank = channel } : null,
            echannel = paymentType == "echannel"
                ? new { bill_info1 = "Pembayaran Lapak", bill_info2 = order.OrderNumber }
                : (object?)null
        };
    }

    /// <summary>Method codes arrive as "type:channel" (e.g. "bank_transfer:bca").</summary>
    private static (string PaymentType, string Channel) SplitMethod(PaymentRequest request)
    {
        var raw = request.PaymentMethod;
        if (raw.Contains(':'))
        {
            var parts = raw.Split(':', 2);
            return (parts[0], parts[1]);
        }
        return (raw, request.BankCode);
    }

    private static PaymentState MapState(string transactionStatus) => transactionStatus switch
    {
        "settlement" or "capture" => PaymentState.Paid,
        "deny" or "cancel" or "failure" => PaymentState.Failed,
        "expire" => PaymentState.Expired,
        "refund" or "partial_refund" => PaymentState.Refunded,
        _ => PaymentState.Pending
    };

    private static string ComputeSignature(string orderId, string statusCode, string grossAmount, string serverKey)
    {
        var input = orderId + statusCode + grossAmount + serverKey;
        var hash = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ExtractMidtransError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("status_message", out var msg))
                return msg.GetString() ?? body;
        }
        catch { /* fall through to the raw body */ }
        return Truncate(body, 200);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
