using Lapak.Models;

namespace Lapak.Services.Payment;

/// <summary>
/// Normalised payment lifecycle shared by every gateway. Each provider translates
/// its own vocabulary into this enum exactly once, so the rest of the app never
/// has to know that Midtrans says "settlement" while Stripe says "paid".
/// </summary>
public enum PaymentState
{
    Pending,
    Paid,
    Failed,
    Expired,
    Refunded
}

/// <summary>
/// A payment option offered to the buyer at checkout. Gateways expose different
/// sets, so the checkout page renders whatever the selected gateway reports.
/// </summary>
public record PaymentMethodOption(string Code, string Label, string Group);

public class PaymentRequest
{
    public Guid OrderId { get; set; }

    /// <summary>Midtrans, Xendit, or Stripe. Falls back to the configured default when empty.</summary>
    public string? Gateway { get; set; }

    /// <summary>Gateway-specific method code, e.g. "bank_transfer", "OVO", "card".</summary>
    public string PaymentMethod { get; set; } = "bank_transfer";

    /// <summary>Bank or e-wallet channel for gateways that need one, e.g. "bca".</summary>
    public string BankCode { get; set; } = "bca";
}

public class PaymentResponse
{
    public bool Success { get; set; }
    public string Gateway { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Hosted checkout page the buyer must be redirected to (Xendit, Stripe).</summary>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>Virtual account number the buyer transfers to (Midtrans bank transfer).</summary>
    public string VaNumber { get; set; } = string.Empty;

    /// <summary>Human-readable instruction shown on the confirmation screen.</summary>
    public string Instruction { get; set; } = string.Empty;

    public PaymentState State { get; set; } = PaymentState.Pending;
    public string? ErrorMessage { get; set; }

    public static PaymentResponse Failed(string gateway, string message) =>
        new() { Success = false, Gateway = gateway, ErrorMessage = message };
}

/// <summary>
/// Everything a provider needs to authenticate an inbound webhook. Signature data
/// lives in headers for Xendit and Stripe, and in the body for Midtrans.
/// </summary>
public class PaymentCallbackContext
{
    public string RawBody { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    public string? Header(string name) =>
        Headers.TryGetValue(name, out var value) ? value : null;
}

public class PaymentCallbackResult
{
    public bool Success { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public PaymentState State { get; set; } = PaymentState.Pending;
    public string? ErrorMessage { get; set; }

    /// <summary>True when the signature or token check failed — answer 401, never 200.</summary>
    public bool Unauthorized { get; set; }

    public static PaymentCallbackResult Rejected(string message) =>
        new() { Success = false, Unauthorized = true, ErrorMessage = message };

    public static PaymentCallbackResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// One payment gateway. Register an implementation in Program.cs and it becomes
/// selectable at checkout automatically — PaymentService discovers providers
/// through DI rather than a hardcoded switch.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Stable key used in config, the Order.PaymentGateway column, and callback routes.</summary>
    string Name { get; }

    /// <summary>Name shown to the buyer at checkout.</summary>
    string DisplayName { get; }

    /// <summary>False when credentials are missing, which hides the gateway at checkout.</summary>
    bool IsConfigured { get; }

    IReadOnlyList<PaymentMethodOption> SupportedMethods { get; }

    Task<PaymentResponse> CreatePaymentAsync(Order order, PaymentRequest request, CancellationToken ct = default);

    Task<PaymentCallbackResult> HandleCallbackAsync(PaymentCallbackContext context, CancellationToken ct = default);
}

/// <summary>Gateway summary used to build the checkout picker.</summary>
public record GatewayInfo(string Name, string DisplayName, bool IsConfigured, IReadOnlyList<PaymentMethodOption> Methods);
