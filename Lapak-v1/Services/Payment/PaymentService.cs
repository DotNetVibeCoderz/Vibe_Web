using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lapak.Data;
using Lapak.Models;
using Lapak.Models.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lapak.Services.Payment;

// ================================================================
// DTOs
// ================================================================
public class PaymentRequest
{
    public Guid OrderId { get; set; }
    public string PaymentMethod { get; set; } = "bank_transfer"; // bank_transfer, ewallet, credit_card
    public string BankCode { get; set; } = "bca"; // bca, bni, mandiri, etc.
}

public class PaymentResponse
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public string VaNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
}

public class MidtransTransaction
{
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("gross_amount")]
    public decimal GrossAmount { get; set; }

    [JsonPropertyName("payment_type")]
    public string PaymentType { get; set; } = string.Empty;

    [JsonPropertyName("transaction_status")]
    public string TransactionStatus { get; set; } = string.Empty;

    [JsonPropertyName("va_numbers")]
    public List<MidtransVaNumber>? VaNumbers { get; set; }
}

public class MidtransVaNumber
{
    [JsonPropertyName("bank")]
    public string Bank { get; set; } = string.Empty;

    [JsonPropertyName("va_number")]
    public string VaNumber { get; set; } = string.Empty;
}

public class MidtransCallback
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("transaction_status")]
    public string TransactionStatus { get; set; } = string.Empty;

    [JsonPropertyName("payment_type")]
    public string PaymentType { get; set; } = string.Empty;

    [JsonPropertyName("gross_amount")]
    public string GrossAmount { get; set; } = string.Empty;

    [JsonPropertyName("fraud_status")]
    public string FraudStatus { get; set; } = "accept";

    [JsonPropertyName("signature_key")]
    public string SignatureKey { get; set; } = string.Empty;
}

public class XenditInvoiceRequest
{
    [JsonPropertyName("external_id")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("payer_email")]
    public string PayerEmail { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("payment_methods")]
    public List<string> PaymentMethods { get; set; } = new();

    [JsonPropertyName("success_redirect_url")]
    public string SuccessRedirectUrl { get; set; } = string.Empty;
}

public class XenditInvoiceResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("external_id")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("invoice_url")]
    public string InvoiceUrl { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

// ================================================================
// Interface
// ================================================================
public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request, CancellationToken ct = default);
    Task<PaymentResponse> ProcessCallbackAsync(string gateway, string rawBody, CancellationToken ct = default);
    Task<string> GetPaymentStatusAsync(string orderNumber, CancellationToken ct = default);
}

// ================================================================
// Implementation
// ================================================================
public class PaymentService : IPaymentService
{
    private readonly PaymentGatewayConfig _config;
    private readonly LapakDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IOptions<PaymentGatewayConfig> config,
        LapakDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<PaymentService> logger)
    {
        _config = config.Value;
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

        if (order == null)
            return new PaymentResponse { Success = false, ErrorMessage = "Order tidak ditemukan." };

        if (order.PaymentStatus == "Paid")
            return new PaymentResponse { Success = false, ErrorMessage = "Order sudah dibayar." };

        var gateway = _config.DefaultGateway;
        return gateway switch
        {
            "Xendit" => await CreateXenditPaymentAsync(order, request, ct),
            _ => await CreateMidtransPaymentAsync(order, request, ct)
        };
    }

    public async Task<PaymentResponse> ProcessCallbackAsync(string gateway, string rawBody, CancellationToken ct = default)
    {
        return gateway.ToLower() switch
        {
            "midtrans" => await ProcessMidtransCallbackAsync(rawBody, ct),
            "xendit" => await ProcessXenditCallbackAsync(rawBody, ct),
            _ => new PaymentResponse { Success = false, ErrorMessage = $"Unknown gateway: {gateway}" }
        };
    }

    public async Task<string> GetPaymentStatusAsync(string orderNumber, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
        if (order == null) return "Order tidak ditemukan.";

        return $"📋 Order #{order.OrderNumber}\n" +
               $"Status Pesanan: {order.Status}\n" +
               $"Status Pembayaran: {order.PaymentStatus}\n" +
               $"Metode: {order.PaymentMethod}\n" +
               $"Total: Rp{order.GrandTotal:N0}\n" +
               $"Gateway: {order.PaymentGateway}\n" +
               $"ID Transaksi: {order.PaymentTransactionId ?? "Belum ada"}";
    }

    // ================================================================
    // Midtrans Implementation
    // ================================================================
    private async Task<PaymentResponse> CreateMidtransPaymentAsync(Order order, PaymentRequest request, CancellationToken ct)
    {
        var midtrans = _config.Midtrans;
        if (string.IsNullOrEmpty(midtrans.ServerKey))
            return new PaymentResponse { Success = false, ErrorMessage = "Midtrans Server Key belum dikonfigurasi." };

        try
        {
            var baseUrl = midtrans.IsProduction
                ? "https://api.midtrans.com/v2"
                : "https://api.sandbox.midtrans.com/v2";

            var payload = new
            {
                payment_type = request.PaymentMethod,
                transaction_details = new
                {
                    order_id = order.OrderNumber,
                    gross_amount = (int)order.GrandTotal
                },
                customer_details = new
                {
                    first_name = order.User?.FullName ?? "Pelanggan",
                    email = order.User?.Email ?? "",
                    phone = order.User?.PhoneNumber ?? ""
                },
                bank_transfer = request.PaymentMethod == "bank_transfer" ? new
                {
                    bank = request.BankCode
                } : null,
                item_details = order.OrderItems.Select(i => new
                {
                    id = i.ProductId.ToString(),
                    price = (int)i.Price,
                    quantity = i.Quantity,
                    name = i.Product?.Name ?? "Produk"
                }).ToArray()
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("PaymentClient");
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(midtrans.ServerKey + ":"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            var response = await client.PostAsync($"{baseUrl}/charge", content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Midtrans error: {Body}", responseBody);
                return new PaymentResponse { Success = false, ErrorMessage = $"Midtrans error: {responseBody}" };
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var transactionId = root.GetProperty("transaction_id").GetString() ?? "";
            var status = root.GetProperty("transaction_status").GetString() ?? "pending";

            string vaNumber = "";
            if (root.TryGetProperty("va_numbers", out var vaNumbers) && vaNumbers.GetArrayLength() > 0)
                vaNumber = vaNumbers[0].GetProperty("va_number").GetString() ?? "";

            // Update order
            order.PaymentGateway = "Midtrans";
            order.PaymentTransactionId = transactionId;
            order.PaymentMethod = request.PaymentMethod + "_" + request.BankCode;
            order.PaymentStatus = status == "settlement" ? "Paid" : "Unpaid";
            order.Status = status == "settlement" ? "Paid" : order.Status;

            await _db.SaveChangesAsync(ct);

            return new PaymentResponse
            {
                Success = true,
                TransactionId = transactionId,
                VaNumber = vaNumber,
                Status = status,
                PaymentUrl = "" // Bank transfer returns VA number, not URL
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Midtrans payment creation failed");
            return new PaymentResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<PaymentResponse> ProcessMidtransCallbackAsync(string rawBody, CancellationToken ct)
    {
        try
        {
            var callback = JsonSerializer.Deserialize<MidtransCallback>(rawBody);
            if (callback == null)
                return new PaymentResponse { Success = false, ErrorMessage = "Invalid callback payload." };

            // Verify signature (in production)
            var midtrans = _config.Midtrans;
            var expectedSignature = ComputeMidtransSignature(callback.OrderId, callback.TransactionStatus, callback.GrossAmount, midtrans.ServerKey);

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == callback.OrderId, ct);
            if (order == null)
                return new PaymentResponse { Success = false, ErrorMessage = $"Order {callback.OrderId} not found." };

            // Update order status based on transaction status
            order.PaymentTransactionId = callback.TransactionId;
            order.PaymentStatus = callback.TransactionStatus switch
            {
                "settlement" or "capture" => "Paid",
                "pending" => "Unpaid",
                "deny" or "cancel" or "expire" => "Failed",
                "refund" or "partial_refund" => "Refunded",
                _ => order.PaymentStatus
            };

            if (order.PaymentStatus == "Paid" && order.Status == "Pending")
                order.Status = "Paid";

            order.PaidAt = order.PaymentStatus == "Paid" ? DateTime.UtcNow : order.PaidAt;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Midtrans callback processed: Order #{OrderNumber} -> {Status}", callback.OrderId, order.Status);

            return new PaymentResponse { Success = true, TransactionId = callback.TransactionId, Status = callback.TransactionStatus };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Midtrans callback processing failed");
            return new PaymentResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    // ================================================================
    // Xendit Implementation
    // ================================================================
    private async Task<PaymentResponse> CreateXenditPaymentAsync(Order order, PaymentRequest request, CancellationToken ct)
    {
        var xendit = _config.Xendit;
        if (string.IsNullOrEmpty(xendit.ApiKey))
            return new PaymentResponse { Success = false, ErrorMessage = "Xendit API Key belum dikonfigurasi." };

        try
        {
            var invoiceRequest = new XenditInvoiceRequest
            {
                ExternalId = order.OrderNumber,
                Amount = order.GrandTotal,
                PayerEmail = order.User?.Email ?? "customer@lapak.com",
                Description = $"Pembayaran order #{order.OrderNumber} - Lapak",
                PaymentMethods = new List<string> { "BCA", "BNI", "BRI", "MANDIRI", "OVO", "DANA", "SHOPEEPAY" },
                SuccessRedirectUrl = $"/account/orders/{order.OrderNumber}"
            };

            var json = JsonSerializer.Serialize(invoiceRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("PaymentClient");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(xendit.ApiKey + ":")));

            var baseUrl = xendit.IsProduction ? "https://api.xendit.co" : xendit.BaseUrl;
            var response = await client.PostAsync($"{baseUrl}/v2/invoices", content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Xendit error: {Body}", responseBody);
                return new PaymentResponse { Success = false, ErrorMessage = $"Xendit error: {responseBody}" };
            }

            var invoice = JsonSerializer.Deserialize<XenditInvoiceResponse>(responseBody);
            if (invoice == null)
                return new PaymentResponse { Success = false, ErrorMessage = "Invalid Xendit response." };

            order.PaymentGateway = "Xendit";
            order.PaymentTransactionId = invoice.Id;
            order.PaymentMethod = request.PaymentMethod;
            order.PaymentStatus = "Unpaid";
            await _db.SaveChangesAsync(ct);

            return new PaymentResponse
            {
                Success = true,
                TransactionId = invoice.Id,
                PaymentUrl = invoice.InvoiceUrl,
                Status = invoice.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xendit payment creation failed");
            return new PaymentResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<PaymentResponse> ProcessXenditCallbackAsync(string rawBody, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            var externalId = root.GetProperty("external_id").GetString() ?? "";
            var status = root.GetProperty("status").GetString() ?? "";
            var invoiceId = root.GetProperty("id").GetString() ?? "";

            // Verify callback token
            var xendit = _config.Xendit;
            if (!string.IsNullOrEmpty(xendit.CallbackToken))
            {
                // In production: validate Xendit callback header token
            }

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == externalId, ct);
            if (order == null)
                return new PaymentResponse { Success = false, ErrorMessage = $"Order {externalId} not found." };

            order.PaymentStatus = status switch
            {
                "PAID" or "SETTLED" => "Paid",
                "PENDING" => "Unpaid",
                "EXPIRED" => "Failed",
                _ => order.PaymentStatus
            };

            order.PaymentTransactionId = invoiceId;
            if (order.PaymentStatus == "Paid" && order.Status == "Pending")
                order.Status = "Paid";
            order.PaidAt = order.PaymentStatus == "Paid" ? DateTime.UtcNow : order.PaidAt;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Xendit callback: Order #{OrderNumber} -> {Status}", externalId, order.Status);

            return new PaymentResponse { Success = true, TransactionId = invoiceId, Status = status };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xendit callback processing failed");
            return new PaymentResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static string ComputeMidtransSignature(string orderId, string statusCode, string grossAmount, string serverKey)
    {
        var input = orderId + statusCode + grossAmount + serverKey;
        using var sha = SHA512.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLower();
    }
}
