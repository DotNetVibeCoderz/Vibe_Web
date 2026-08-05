using SMSNet.Models;

namespace SMSNet.Services.Payments;

/// <summary>What the payer is being asked to pay.</summary>
public sealed record ChargeRequest(
    string Reference,
    string StudentName,
    string Category,
    decimal Amount,
    string? PayerEmail,
    string? PayerPhone);

/// <summary>The gateway's answer: where to send the payer and what to tell them.</summary>
public sealed record ChargeResult(
    bool Success,
    PaymentStatus Status,
    string? ExternalId,
    string? PaymentTarget,
    string? Instructions,
    string? FailureReason)
{
    public static ChargeResult Ok(PaymentStatus status, string? externalId, string? target, string? instructions) =>
        new(true, status, externalId, target, instructions, null);

    public static ChargeResult Fail(string reason) =>
        new(false, PaymentStatus.Failed, null, null, null, reason);
}

/// <summary>
/// A way to collect money.
/// <para>
/// Implementations are resolved by <see cref="PaymentGatewayRegistry"/> from the
/// effective configuration, so adding a provider means adding one class and one
/// registry entry — no page changes.
/// </para>
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Stable key matching the configuration entry.</summary>
    string Key { get; }

    /// <summary>How the payer completes payment; drives the checkout UI.</summary>
    PaymentChannelKind Channel { get; }

    /// <summary>Whether this provider can run without live credentials.</summary>
    bool RequiresCredentials { get; }

    Task<ChargeResult> CreateChargeAsync(
        ChargeRequest request,
        PaymentGatewayConfig config,
        CancellationToken cancellationToken = default);
}
