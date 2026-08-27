using Lapak.Data;
using Lapak.Models;
using Lapak.Models.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lapak.Services.Payment;

public interface IPaymentService
{
    /// <summary>Gateways that can be offered at checkout, configured ones first.</summary>
    IReadOnlyList<GatewayInfo> GetAvailableGateways();

    /// <summary>Methods offered by one gateway, for the checkout method picker.</summary>
    IReadOnlyList<PaymentMethodOption> GetMethods(string gateway);

    Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request, CancellationToken ct = default);

    Task<PaymentCallbackResult> ProcessCallbackAsync(string gateway, PaymentCallbackContext context, CancellationToken ct = default);

    Task<string> GetPaymentStatusAsync(string orderNumber, CancellationToken ct = default);
}

/// <summary>
/// Routes a payment to the gateway the buyer picked and writes the result back to
/// the order. Providers own their protocol details; this class owns the order
/// bookkeeping so status handling stays identical across gateways.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IReadOnlyDictionary<string, IPaymentProvider> _providers;
    private readonly PaymentGatewayConfig _config;
    private readonly LapakDbContext _db;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IEnumerable<IPaymentProvider> providers,
        IOptions<PaymentGatewayConfig> config,
        LapakDbContext db,
        ILogger<PaymentService> logger)
    {
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _config = config.Value;
        _db = db;
        _logger = logger;
    }

    public IReadOnlyList<GatewayInfo> GetAvailableGateways() =>
        _providers.Values
            .Select(p => new GatewayInfo(p.Name, p.DisplayName, p.IsConfigured, p.SupportedMethods))
            .OrderByDescending(g => g.IsConfigured)
            .ThenBy(g => g.Name != _config.DefaultGateway)
            .ThenBy(g => g.Name)
            .ToList();

    public IReadOnlyList<PaymentMethodOption> GetMethods(string gateway) =>
        _providers.TryGetValue(gateway, out var provider)
            ? provider.SupportedMethods
            : Array.Empty<PaymentMethodOption>();

    public async Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

        if (order == null)
            return PaymentResponse.Failed(request.Gateway ?? "", "Pesanan tidak ditemukan.");

        if (order.PaymentStatus == "Paid")
            return PaymentResponse.Failed(order.PaymentGateway ?? "", "Pesanan ini sudah dibayar.");

        var gatewayName = string.IsNullOrWhiteSpace(request.Gateway) ? _config.DefaultGateway : request.Gateway;
        if (!_providers.TryGetValue(gatewayName, out var provider))
            return PaymentResponse.Failed(gatewayName, $"Payment gateway '{gatewayName}' tidak dikenal.");

        if (!provider.IsConfigured)
            return PaymentResponse.Failed(provider.Name, $"{provider.Name} belum dikonfigurasi di server.");

        var response = await provider.CreatePaymentAsync(order, request, ct);

        if (response.Success)
        {
            order.PaymentGateway = provider.Name;
            order.PaymentTransactionId = response.TransactionId;
            order.PaymentMethod = request.PaymentMethod;
            ApplyState(order, response.State);
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Payment created via {Gateway} for order {OrderNumber} ({State})",
                provider.Name, order.OrderNumber, response.State);
        }
        else
        {
            _logger.LogWarning(
                "Payment creation failed via {Gateway} for order {OrderNumber}: {Error}",
                provider.Name, order.OrderNumber, response.ErrorMessage);
        }

        return response;
    }

    public async Task<PaymentCallbackResult> ProcessCallbackAsync(string gateway, PaymentCallbackContext context, CancellationToken ct = default)
    {
        if (!_providers.TryGetValue(gateway, out var provider))
            return PaymentCallbackResult.Failed($"Payment gateway '{gateway}' tidak dikenal.");

        var result = await provider.HandleCallbackAsync(context, ct);
        if (!result.Success)
            return result;

        if (string.IsNullOrWhiteSpace(result.OrderNumber))
            return PaymentCallbackResult.Failed("Callback tidak menyertakan nomor pesanan.");

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == result.OrderNumber, ct);
        if (order == null)
            return PaymentCallbackResult.Failed($"Pesanan {result.OrderNumber} tidak ditemukan.");

        order.PaymentGateway = provider.Name;
        if (!string.IsNullOrWhiteSpace(result.TransactionId))
            order.PaymentTransactionId = result.TransactionId;

        ApplyState(order, result.State);
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "{Gateway} callback applied: order {OrderNumber} -> {PaymentStatus}",
            provider.Name, order.OrderNumber, order.PaymentStatus);

        return result;
    }

    public async Task<string> GetPaymentStatusAsync(string orderNumber, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
        if (order == null) return "Pesanan tidak ditemukan.";

        return $"📋 Pesanan #{order.OrderNumber}\n" +
               $"Status pesanan: {order.Status}\n" +
               $"Status pembayaran: {order.PaymentStatus}\n" +
               $"Metode: {order.PaymentMethod}\n" +
               $"Total: Rp{order.GrandTotal:N0}\n" +
               $"Gateway: {order.PaymentGateway ?? "Belum dipilih"}\n" +
               $"ID transaksi: {order.PaymentTransactionId ?? "Belum ada"}";
    }

    /// <summary>
    /// Single place where a gateway state becomes order columns, so Midtrans,
    /// Xendit, and Stripe all move an order through the same transitions.
    /// </summary>
    private static void ApplyState(Order order, PaymentState state)
    {
        switch (state)
        {
            case PaymentState.Paid:
                order.PaymentStatus = "Paid";
                order.PaidAt ??= DateTime.UtcNow;
                if (order.Status == "Pending") order.Status = "Paid";
                break;

            case PaymentState.Failed:
                order.PaymentStatus = "Failed";
                break;

            case PaymentState.Expired:
                order.PaymentStatus = "Failed";
                if (order.Status == "Pending")
                {
                    order.Status = "Cancelled";
                    order.CancelledAt ??= DateTime.UtcNow;
                }
                break;

            case PaymentState.Refunded:
                order.PaymentStatus = "Refunded";
                order.Status = "Refunded";
                break;

            case PaymentState.Pending:
            default:
                if (order.PaymentStatus != "Paid") order.PaymentStatus = "Unpaid";
                break;
        }
    }
}
