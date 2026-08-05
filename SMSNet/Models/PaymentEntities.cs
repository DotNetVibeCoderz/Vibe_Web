using System.ComponentModel.DataAnnotations;

namespace SMSNet.Models;

/// <summary>How the payer completes the payment — drives what the checkout screen shows.</summary>
public enum PaymentChannelKind
{
    /// <summary>Payer is sent to the provider's hosted page.</summary>
    Redirect = 0,
    /// <summary>Payer scans a QR code (QRIS).</summary>
    QrCode = 1,
    /// <summary>Payer transfers to a generated virtual account number.</summary>
    VirtualAccount = 2,
    /// <summary>Payer transfers manually and the school confirms it by hand.</summary>
    ManualTransfer = 3
}

public enum PaymentStatus
{
    Pending = 0,
    AwaitingConfirmation = 1,
    Paid = 2,
    Failed = 3,
    Expired = 4,
    Refunded = 5,
    Cancelled = 6
}

/// <summary>
/// Per-gateway settings that an administrator can change from the UI.
/// <para>
/// Values here override the matching <c>Payments:Gateways</c> entry in
/// appsettings, so a school can switch provider or rotate a key without a
/// redeploy — while a fresh install still boots from configuration alone.
/// </para>
/// </summary>
public class PaymentGatewayConfig
{
    public int Id { get; set; }

    /// <summary>Stable provider key: midtrans, xendit, stripe, qris, manual.</summary>
    [Required, MaxLength(40)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    /// <summary>Order in the payer's channel list; lower shows first.</summary>
    public int SortOrder { get; set; }

    /// <summary>Sandbox mode. Real credentials are never required while true.</summary>
    public bool SandboxMode { get; set; } = true;

    [MaxLength(300)]
    public string? ApiKey { get; set; }

    [MaxLength(300)]
    public string? SecretKey { get; set; }

    [MaxLength(300)]
    public string? MerchantId { get; set; }

    /// <summary>Bank account or QRIS merchant string, for the offline channels.</summary>
    [MaxLength(300)]
    public string? AccountDetail { get; set; }

    [MaxLength(600)]
    public string? Instructions { get; set; }

    public decimal FeeFlat { get; set; }

    /// <summary>Percentage fee, e.g. 2.9 for 2.9%.</summary>
    public decimal FeePercent { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>One attempt to collect one payment.</summary>
public class PaymentTransaction
{
    public int Id { get; set; }

    /// <summary>School-side reference shown to the payer, e.g. SMSNET-20260805-0007.</summary>
    [Required, MaxLength(60)]
    public string Reference { get; set; } = string.Empty;

    /// <summary>Links back to the <see cref="PaymentRecord"/> being settled, when there is one.</summary>
    public int? PaymentRecordId { get; set; }

    [Required, MaxLength(160)]
    public string StudentName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Category { get; set; } = "SPP";

    public decimal Amount { get; set; }

    /// <summary>Provider fee at the time of the charge, so historical totals stay correct.</summary>
    public decimal Fee { get; set; }

    [Required, MaxLength(40)]
    public string GatewayKey { get; set; } = string.Empty;

    [MaxLength(120)]
    public string GatewayName { get; set; } = string.Empty;

    public PaymentChannelKind Channel { get; set; }

    public PaymentStatus Status { get; set; }

    /// <summary>Identifier returned by the provider; blank in sandbox mode.</summary>
    [MaxLength(200)]
    public string? ExternalId { get; set; }

    /// <summary>Hosted checkout URL, QR payload, or virtual account number.</summary>
    [MaxLength(1000)]
    public string? PaymentTarget { get; set; }

    [MaxLength(600)]
    public string? Instructions { get; set; }

    [MaxLength(400)]
    public string? FailureReason { get; set; }

    /// <summary>Who recorded or confirmed this, for the audit trail.</summary>
    [MaxLength(160)]
    public string? HandledBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsSandbox { get; set; }
}
