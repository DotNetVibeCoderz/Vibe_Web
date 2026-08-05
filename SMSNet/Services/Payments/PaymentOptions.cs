namespace SMSNet.Services.Payments;

/// <summary>
/// Payment settings from appsettings. These are the defaults a fresh install
/// boots with; the admin screen writes overrides into
/// <see cref="Models.PaymentGatewayConfig"/> which win at runtime.
/// </summary>
public class PaymentOptions
{
    public const string SectionName = "Payments";

    /// <summary>ISO currency code used for display formatting.</summary>
    public string Currency { get; set; } = "IDR";

    /// <summary>Prefix for generated payment references.</summary>
    public string ReferencePrefix { get; set; } = "SMSNET";

    /// <summary>How long an unpaid charge stays open.</summary>
    public int ExpiryHours { get; set; } = 24;

    /// <summary>
    /// Global sandbox switch. While true no provider call leaves the building —
    /// charges are simulated locally so the flow can be exercised end to end
    /// without live credentials.
    /// </summary>
    public bool SandboxMode { get; set; } = true;

    public List<GatewaySettings> Gateways { get; set; } = new();

    public class GatewaySettings
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public int SortOrder { get; set; }
        public bool SandboxMode { get; set; } = true;
        public string? ApiKey { get; set; }
        public string? SecretKey { get; set; }
        public string? MerchantId { get; set; }
        public string? AccountDetail { get; set; }
        public string? Instructions { get; set; }
        public decimal FeeFlat { get; set; }
        public decimal FeePercent { get; set; }
    }
}
