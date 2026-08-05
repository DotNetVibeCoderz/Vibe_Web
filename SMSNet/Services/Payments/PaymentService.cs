using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SMSNet.Data;
using SMSNet.Models;

namespace SMSNet.Services.Payments;

/// <summary>
/// Resolves the gateways a school can actually use right now.
/// <para>
/// Effective configuration is appsettings as the baseline, overlaid by any
/// <see cref="PaymentGatewayConfig"/> row an administrator has saved. That split
/// lets a fresh install boot from configuration while a running school changes
/// providers from the UI.
/// </para>
/// </summary>
public sealed class PaymentGatewayRegistry
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IOptionsMonitor<PaymentOptions> _options;
    private readonly Dictionary<string, IPaymentGateway> _gateways;

    public PaymentGatewayRegistry(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IOptionsMonitor<PaymentOptions> options,
        IHttpClientFactory httpFactory,
        ILoggerFactory loggerFactory)
    {
        _dbFactory = dbFactory;
        _options = options;

        _gateways = new IPaymentGateway[]
        {
            new MidtransGateway(httpFactory, loggerFactory.CreateLogger<MidtransGateway>()),
            new XenditGateway(httpFactory, loggerFactory.CreateLogger<XenditGateway>()),
            new StripeGateway(httpFactory, loggerFactory.CreateLogger<StripeGateway>()),
            new QrisGateway(),
            new ManualTransferGateway()
        }.ToDictionary(g => g.Key, StringComparer.OrdinalIgnoreCase);
    }

    public PaymentOptions Options => _options.CurrentValue;

    public IReadOnlyCollection<string> KnownKeys => _gateways.Keys.ToArray();

    public IPaymentGateway? Find(string key) =>
        _gateways.TryGetValue(key, out var gateway) ? gateway : null;

    /// <summary>Every known provider with its effective settings, enabled or not.</summary>
    public async Task<List<PaymentGatewayConfig>> GetAllConfigsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var saved = await db.PaymentGatewayConfigs.AsNoTracking().ToListAsync(ct);
        var defaults = _options.CurrentValue.Gateways;

        var result = new List<PaymentGatewayConfig>();

        foreach (var key in _gateways.Keys)
        {
            var row = saved.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));
            if (row is not null)
            {
                result.Add(row);
                continue;
            }

            var fallback = defaults.FirstOrDefault(d => string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase));
            result.Add(FromSettings(key, fallback, _options.CurrentValue.SandboxMode));
        }

        return result.OrderBy(c => c.SortOrder).ThenBy(c => c.DisplayName).ToList();
    }

    /// <summary>Only the providers a payer may currently choose from.</summary>
    public async Task<List<PaymentGatewayConfig>> GetEnabledConfigsAsync(CancellationToken ct = default) =>
        (await GetAllConfigsAsync(ct)).Where(c => c.Enabled).ToList();

    public async Task<PaymentGatewayConfig?> GetConfigAsync(string key, CancellationToken ct = default) =>
        (await GetAllConfigsAsync(ct))
        .FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));

    public async Task SaveConfigAsync(PaymentGatewayConfig config, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.PaymentGatewayConfigs
            .FirstOrDefaultAsync(c => c.Key == config.Key, ct);

        if (existing is null)
        {
            config.Id = 0;
            config.UpdatedAt = SchoolClock.LocalNow;
            db.PaymentGatewayConfigs.Add(config);
        }
        else
        {
            existing.DisplayName = config.DisplayName;
            existing.Enabled = config.Enabled;
            existing.SortOrder = config.SortOrder;
            existing.SandboxMode = config.SandboxMode;
            existing.ApiKey = config.ApiKey;
            existing.SecretKey = config.SecretKey;
            existing.MerchantId = config.MerchantId;
            existing.AccountDetail = config.AccountDetail;
            existing.Instructions = config.Instructions;
            existing.FeeFlat = config.FeeFlat;
            existing.FeePercent = config.FeePercent;
            existing.UpdatedAt = SchoolClock.LocalNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private static PaymentGatewayConfig FromSettings(string key, PaymentOptions.GatewaySettings? s, bool globalSandbox) =>
        new()
        {
            Key = key,
            DisplayName = s?.DisplayName ?? DefaultName(key),
            Enabled = s?.Enabled ?? key is "manual",
            SortOrder = s?.SortOrder ?? 100,
            SandboxMode = globalSandbox || (s?.SandboxMode ?? true),
            ApiKey = s?.ApiKey,
            SecretKey = s?.SecretKey,
            MerchantId = s?.MerchantId,
            AccountDetail = s?.AccountDetail,
            Instructions = s?.Instructions,
            FeeFlat = s?.FeeFlat ?? 0,
            FeePercent = s?.FeePercent ?? 0,
            UpdatedAt = SchoolClock.LocalNow
        };

    private static string DefaultName(string key) => key switch
    {
        "midtrans" => "Midtrans",
        "xendit" => "Xendit",
        "stripe" => "Stripe",
        "qris" => "QRIS",
        "manual" => "Transfer Manual",
        _ => key
    };
}

/// <summary>Creates charges and reconciles them against the school's ledger.</summary>
public sealed class PaymentService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly PaymentGatewayRegistry _registry;
    private readonly AuditService _audit;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        PaymentGatewayRegistry registry,
        AuditService audit,
        ILogger<PaymentService> logger)
    {
        _dbFactory = dbFactory;
        _registry = registry;
        _audit = audit;
        _logger = logger;
    }

    public PaymentOptions Options => _registry.Options;

    public async Task<List<PaymentTransaction>> GetTransactionsAsync(
        string? studentName = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.PaymentTransactions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(studentName))
        {
            query = query.Where(t => t.StudentName.Contains(studentName));
        }

        return await query.OrderByDescending(t => t.Id).Take(200).ToListAsync(ct);
    }

    /// <summary>Opens a charge on the chosen gateway and records it.</summary>
    public async Task<PaymentTransaction> CreateChargeAsync(
        string gatewayKey,
        string studentName,
        string category,
        decimal amount,
        int? paymentRecordId = null,
        string? payerEmail = null,
        string? payerPhone = null,
        CancellationToken ct = default)
    {
        var config = await _registry.GetConfigAsync(gatewayKey, ct)
                     ?? throw new InvalidOperationException($"Metode pembayaran '{gatewayKey}' tidak dikenal.");

        if (!config.Enabled)
        {
            throw new InvalidOperationException($"Metode pembayaran {config.DisplayName} sedang tidak aktif.");
        }

        var gateway = _registry.Find(gatewayKey)
                      ?? throw new InvalidOperationException($"Metode pembayaran '{gatewayKey}' tidak tersedia.");

        var reference = await NextReferenceAsync(ct);
        var request = new ChargeRequest(reference, studentName, category, amount, payerEmail, payerPhone);
        var result = await gateway.CreateChargeAsync(request, config, ct);

        var fee = Math.Round(config.FeeFlat + amount * config.FeePercent / 100m, 0, MidpointRounding.AwayFromZero);

        var transaction = new PaymentTransaction
        {
            Reference = reference,
            PaymentRecordId = paymentRecordId,
            StudentName = studentName,
            Category = category,
            Amount = amount,
            Fee = fee,
            GatewayKey = config.Key,
            GatewayName = config.DisplayName,
            Channel = gateway.Channel,
            Status = result.Success ? result.Status : PaymentStatus.Failed,
            ExternalId = result.ExternalId,
            PaymentTarget = result.PaymentTarget,
            Instructions = result.Instructions,
            FailureReason = result.FailureReason,
            CreatedAt = SchoolClock.LocalNow,
            ExpiresAt = SchoolClock.LocalNow.AddHours(Math.Max(1, _registry.Options.ExpiryHours)),
            IsSandbox = config.SandboxMode
        };

        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            db.PaymentTransactions.Add(transaction);
            await db.SaveChangesAsync(ct);
        }

        await _audit.WriteAsync("Buat tagihan",
            $"{reference} · {studentName} · {category} · {amount:N0} via {config.DisplayName}");

        return transaction;
    }

    /// <summary>
    /// Marks a charge paid and settles the linked ledger row.
    /// Used by the manual/QRIS channels and by sandbox confirmations.
    /// </summary>
    public async Task<bool> ConfirmAsync(int transactionId, string handledBy, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var transaction = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == transactionId, ct);
        if (transaction is null || transaction.Status == PaymentStatus.Paid)
        {
            return false;
        }

        transaction.Status = PaymentStatus.Paid;
        transaction.PaidAt = SchoolClock.LocalNow;
        transaction.HandledBy = handledBy;

        // Keep the finance ledger in step — otherwise Manajemen Keuangan and the
        // parent portal disagree about whether the bill is settled.
        if (transaction.PaymentRecordId is { } recordId)
        {
            var record = await db.PaymentRecords.FirstOrDefaultAsync(p => p.Id == recordId, ct);
            if (record is not null)
            {
                record.Status = "Paid";
            }
        }

        await db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Konfirmasi pembayaran",
            $"{transaction.Reference} · {transaction.StudentName} · {transaction.Amount:N0}");

        return true;
    }

    public async Task<bool> CancelAsync(int transactionId, string handledBy, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var transaction = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == transactionId, ct);
        if (transaction is null || transaction.Status == PaymentStatus.Paid)
        {
            return false;
        }

        transaction.Status = PaymentStatus.Cancelled;
        transaction.HandledBy = handledBy;
        await db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Batalkan tagihan", $"{transaction.Reference} · {transaction.StudentName}");
        return true;
    }

    /// <summary>Sequential per-day reference: SMSNET-20260805-0007.</summary>
    private async Task<string> NextReferenceAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var today = SchoolClock.Today;
        var todayCount = await db.PaymentTransactions.CountAsync(t => t.CreatedAt >= today, ct);

        return $"{_registry.Options.ReferencePrefix}-{today:yyyyMMdd}-{todayCount + 1:0000}";
    }
}
