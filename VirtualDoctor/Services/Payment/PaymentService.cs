using Microsoft.EntityFrameworkCore;
using QRCoder;
using VirtualDoctor.Data;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services.Payment;

public record PaymentRequest(
    PaymentReferenceType ReferenceType,
    string ReferenceId,
    string UserId,
    string Description,
    decimal Amount,
    decimal Discount = 0,
    decimal InsuranceCoverage = 0);

/// <summary>Hasil penerapan status dari penyedia. <paramref name="Changed"/> false berarti
/// kiriman sah tetapi tidak mengubah apa pun — biasanya karena statusnya sudah sama.</summary>
public record ExternalStatusResult(bool Found, bool Changed, string Message);

public interface IPaymentService
{
    string ActiveProvider { get; }
    bool IsEnabled { get; }
    IReadOnlyList<string> ConfiguredProviders { get; }
    /// <summary>Cara bayar yang tersedia pada penyedia aktif.</summary>
    IReadOnlyList<PaymentChannel> AvailableChannels { get; }
    MerchantInfo Merchant { get; }
    ManualPaymentConfig? ManualInstructions { get; }

    Task<Models.Payment> CreateAsync(PaymentRequest request, PaymentChannel channel, CancellationToken ct = default);
    Task<Models.Payment?> GetAsync(string paymentId);
    Task<Models.Payment?> GetByInvoiceAsync(string invoiceNumber);
    Task<Models.Payment?> GetForReferenceAsync(PaymentReferenceType type, string referenceId);
    Task<List<Models.Payment>> GetForUserAsync(string userId);
    Task<List<Models.Payment>> GetAllAsync();

    /// <summary>Pasien mengunggah bukti transfer, status menjadi menunggu verifikasi.</summary>
    Task<bool> SubmitProofAsync(string paymentId, string proofUrl, string? note);
    /// <summary>Petugas menyetujui atau menolak pembayaran manual.</summary>
    Task<bool> VerifyAsync(string paymentId, bool approved, string verifiedBy, string? note);
    /// <summary>Tanya status ke penyedia lalu simpan bila berubah.</summary>
    Task<PaymentState?> RefreshStatusAsync(string paymentId, CancellationToken ct = default);
    /// <summary>Dipanggil webhook penyedia.</summary>
    Task<ExternalStatusResult> ApplyExternalStatusAsync(string invoiceNumber, PaymentState state, string? externalId);
    /// <summary>Tandai tagihan yang lewat batas waktu.</summary>
    Task<int> ExpireOverdueAsync();

    /// <summary>Gambar QR sebagai data URI PNG, siap dipasang di tag img.</summary>
    string? RenderQrImage(string? payload, int pixelsPerModule = 8);
    (bool Ok, string Message) ValidateQrisConfig();
}

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly PaymentConfig _cfg;
    private readonly Dictionary<string, IPaymentProvider> _providers;
    private readonly ILogger<PaymentService> _log;

    public PaymentService(AppDbContext db, AppConfig config, IHttpClientFactory http, ILoggerFactory lf)
    {
        _db = db;
        _cfg = config.Payment;
        _log = lf.CreateLogger<PaymentService>();
        _providers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Manual"] = new ManualPaymentProvider(_cfg),
            ["Qris"] = new QrisPaymentProvider(_cfg, lf.CreateLogger<QrisPaymentProvider>()),
            ["Midtrans"] = new MidtransPaymentProvider(_cfg, http, lf.CreateLogger<MidtransPaymentProvider>()),
            ["Xendit"] = new XenditPaymentProvider(_cfg, http, lf.CreateLogger<XenditPaymentProvider>())
        };
    }

    public string ActiveProvider => string.IsNullOrWhiteSpace(_cfg.Provider) ? "Manual" : _cfg.Provider;
    public bool IsEnabled => _cfg.Enabled;
    public MerchantInfo Merchant => _cfg.Merchant;
    public ManualPaymentConfig? ManualInstructions => _cfg.Manual;

    public IReadOnlyList<string> ConfiguredProviders =>
        _providers.Where(p => p.Value.IsConfigured).Select(p => p.Key).ToList();

    public IReadOnlyList<PaymentChannel> AvailableChannels
    {
        get
        {
            var channels = new List<PaymentChannel>();
            if (_providers.TryGetValue(ActiveProvider, out var active) && active.IsConfigured)
                channels.AddRange(active.SupportedChannels);

            // Transfer manual selalu tersedia sebagai jalan keluar bila penyedia bermasalah.
            if (_providers["Manual"].IsConfigured)
                channels.AddRange(_providers["Manual"].SupportedChannels);

            return channels.Distinct().ToList();
        }
    }

    // ============ Pembuatan tagihan ============

    public async Task<Models.Payment> CreateAsync(PaymentRequest request, PaymentChannel channel, CancellationToken ct = default)
    {
        // Satu transaksi cukup punya satu tagihan aktif.
        var existing = await _db.Payments
            .FirstOrDefaultAsync(p => p.ReferenceType == request.ReferenceType
                                   && p.ReferenceId == request.ReferenceId
                                   && (p.State == PaymentState.Pending || p.State == PaymentState.AwaitingVerification || p.State == PaymentState.Paid), ct);

        if (existing != null && existing.State == PaymentState.Paid) return existing;

        var payment = existing ?? new Models.Payment
        {
            InvoiceNumber = await NextInvoiceNumberAsync(ct),
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        payment.Description = request.Description;
        payment.Amount = request.Amount;
        payment.Discount = request.Discount;
        payment.InsuranceCoverage = request.InsuranceCoverage;
        payment.ServiceFee = _cfg.ServiceFee;
        payment.Total = Math.Max(0, request.Amount - request.Discount - request.InsuranceCoverage + _cfg.ServiceFee);
        payment.Channel = channel;
        payment.State = PaymentState.Pending;

        // Kanal manual selalu ditangani penyedia manual, apa pun penyedia aktifnya.
        var providerName = channel is PaymentChannel.BankTransfer or PaymentChannel.Cash
            ? "Manual"
            : ActiveProvider;

        if (!_providers.TryGetValue(providerName, out var provider) || !provider.IsConfigured)
        {
            provider = _providers["Manual"];
            providerName = "Manual";
            _log.LogWarning("[Payment] Penyedia {P} tidak siap, tagihan {Inv} dialihkan ke transfer manual",
                ActiveProvider, payment.InvoiceNumber);
        }

        try
        {
            var result = await provider.CreateChargeAsync(payment, ct);
            payment.Provider = result.Provider;
            payment.Channel = result.Channel;
            payment.ExternalId = result.ExternalId;
            payment.QrPayload = result.QrPayload;
            payment.PaymentUrl = result.PaymentUrl;
            payment.VirtualAccountNumber = result.VirtualAccountNumber;
            payment.ExpiresAt = result.ExpiresAt ?? DateTime.UtcNow.AddMinutes(_cfg.ExpiryMinutes);
        }
        catch (Exception ex)
        {
            // Tagihan tetap dibuat agar pasien bisa membayar lewat transfer manual.
            _log.LogError(ex, "[Payment] Gagal membuat tagihan di {P}, dialihkan ke transfer manual", providerName);
            payment.Provider = "Manual";
            payment.Channel = PaymentChannel.BankTransfer;
            payment.ExpiresAt = DateTime.UtcNow.AddMinutes(_cfg.ExpiryMinutes);
        }

        if (existing == null) _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("[Payment] Tagihan {Inv} sebesar {Total} dibuat lewat {Provider}",
            payment.InvoiceNumber, payment.Total, payment.Provider);

        return payment;
    }

    /// <summary>
    /// Nomor urut per bulan: INV/2026/07/0001. Urutannya diambil dari tabel
    /// penghitung tersendiri (lihat <see cref="InvoiceNumbering"/>) supaya dua
    /// checkout bersamaan tidak pernah mendapat nomor yang sama.
    /// </summary>
    private async Task<string> NextInvoiceNumberAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var prefix = $"{_cfg.InvoicePrefix}/{now:yyyy}/{now:MM}/";
        var sequence = await InvoiceNumbering.NextAsync(_db, prefix, ct);
        return prefix + sequence.ToString("D4");
    }

    // ============ Pembacaan ============

    public async Task<Models.Payment?> GetAsync(string paymentId) =>
        await _db.Payments.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == paymentId);

    public async Task<Models.Payment?> GetByInvoiceAsync(string invoiceNumber) =>
        await _db.Payments.Include(p => p.User).FirstOrDefaultAsync(p => p.InvoiceNumber == invoiceNumber);

    public async Task<Models.Payment?> GetForReferenceAsync(PaymentReferenceType type, string referenceId) =>
        await _db.Payments.Include(p => p.User)
            .Where(p => p.ReferenceType == type && p.ReferenceId == referenceId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<List<Models.Payment>> GetForUserAsync(string userId) =>
        await _db.Payments.Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt).ToListAsync();

    public async Task<List<Models.Payment>> GetAllAsync() =>
        await _db.Payments.AsNoTracking().Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt).ToListAsync();

    // ============ Verifikasi ============

    public async Task<bool> SubmitProofAsync(string paymentId, string proofUrl, string? note)
    {
        var payment = await _db.Payments.FindAsync(paymentId);
        if (payment == null || payment.State == PaymentState.Paid) return false;

        payment.ProofUrl = proofUrl;
        payment.PayerNote = note;
        payment.State = PaymentState.AwaitingVerification;
        await _db.SaveChangesAsync();

        _log.LogInformation("[Payment] Bukti bayar {Inv} diunggah", payment.InvoiceNumber);
        return true;
    }

    public async Task<bool> VerifyAsync(string paymentId, bool approved, string verifiedBy, string? note)
    {
        var payment = await _db.Payments.FindAsync(paymentId);
        if (payment == null) return false;

        payment.State = approved ? PaymentState.Paid : PaymentState.Failed;
        payment.VerifiedBy = verifiedBy;
        payment.VerifiedAt = DateTime.UtcNow;
        payment.VerificationNote = note;
        if (approved) payment.PaidAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await SyncReferenceAsync(payment);

        _log.LogInformation("[Payment] {Inv} {Hasil} oleh {Petugas}",
            payment.InvoiceNumber, approved ? "disetujui" : "ditolak", verifiedBy);
        return true;
    }

    public async Task<PaymentState?> RefreshStatusAsync(string paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.FindAsync(new object?[] { paymentId }, ct);
        if (payment == null) return null;
        if (!_providers.TryGetValue(payment.Provider, out var provider)) return null;

        try
        {
            var state = await provider.QueryStatusAsync(payment, ct);
            if (state == null || state == payment.State) return state;

            payment.State = state.Value;
            if (state == PaymentState.Paid && payment.PaidAt == null) payment.PaidAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await SyncReferenceAsync(payment);
            return state;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[Payment] Gagal menanyakan status {Inv}", payment.InvoiceNumber);
            return null;
        }
    }

    public async Task<ExternalStatusResult> ApplyExternalStatusAsync(string invoiceNumber, PaymentState state, string? externalId)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.InvoiceNumber == invoiceNumber);
        if (payment == null)
            return new ExternalStatusResult(false, false, $"Tagihan {invoiceNumber} tidak ditemukan.");

        // Id penyedia tetap dicatat walau statusnya tidak berubah, karena berguna
        // saat mencocokkan dengan laporan settlement.
        if (!string.IsNullOrEmpty(externalId) && payment.ExternalId != externalId)
            payment.ExternalId = externalId;

        if (payment.State == state)
        {
            await _db.SaveChangesAsync();
            return new ExternalStatusResult(true, false, $"Status sudah {PaymentLabels.State(state)}, tidak ada perubahan.");
        }

        if (!CanTransition(payment.State, state))
        {
            await _db.SaveChangesAsync();
            _log.LogWarning("[Payment] Notifikasi {Inv} ditolak: {From} tidak boleh menjadi {To}",
                invoiceNumber, payment.State, state);
            return new ExternalStatusResult(true, false,
                $"Perubahan {PaymentLabels.State(payment.State)} → {PaymentLabels.State(state)} tidak diizinkan.");
        }

        payment.State = state;
        if (state == PaymentState.Paid && payment.PaidAt == null) payment.PaidAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await SyncReferenceAsync(payment);

        _log.LogInformation("[Payment] Status {Inv} diperbarui penyedia menjadi {State}", invoiceNumber, state);
        return new ExternalStatusResult(true, true, $"Status menjadi {PaymentLabels.State(state)}.");
    }

    /// <summary>
    /// Perubahan status yang boleh datang dari penyedia. Kiriman ulang yang terlambat
    /// tidak boleh memundurkan status: tagihan yang sudah lunas hanya bisa berpindah
    /// ke Dikembalikan, dan tagihan yang sudah dikembalikan tidak berpindah lagi.
    /// Pelunasan yang datang setelah kedaluwarsa tetap diterima karena hal itu memang terjadi.
    /// </summary>
    private static bool CanTransition(PaymentState from, PaymentState to) => from switch
    {
        PaymentState.Paid => to == PaymentState.Refunded,
        PaymentState.Refunded => false,
        PaymentState.Expired or PaymentState.Failed => to is PaymentState.Paid or PaymentState.Refunded,
        _ => true
    };

    public async Task<int> ExpireOverdueAsync()
    {
        var now = DateTime.UtcNow;
        var overdue = await _db.Payments
            .Where(p => p.State == PaymentState.Pending && p.ExpiresAt != null && p.ExpiresAt < now)
            .ToListAsync();

        foreach (var payment in overdue) payment.State = PaymentState.Expired;
        if (overdue.Count > 0) await _db.SaveChangesAsync();
        return overdue.Count;
    }

    /// <summary>Selaraskan status transaksi asal setelah pembayaran diterima.</summary>
    private async Task SyncReferenceAsync(Models.Payment payment)
    {
        if (payment.State != PaymentState.Paid) return;

        switch (payment.ReferenceType)
        {
            case PaymentReferenceType.Order:
                var order = await _db.Orders.FindAsync(payment.ReferenceId);
                if (order != null)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                    if (order.Status == OrderStatus.Pending) order.Status = OrderStatus.Confirmed;
                }
                break;

            case PaymentReferenceType.Appointment:
                var appointment = await _db.Appointments.FindAsync(payment.ReferenceId);
                if (appointment is { Status: AppointmentStatus.Scheduled })
                    appointment.Status = AppointmentStatus.Confirmed;
                break;

            case PaymentReferenceType.Homecare:
                var homecare = await _db.HomecareServices.FindAsync(payment.ReferenceId);
                if (homecare is { Status: HomecareServiceStatus.Requested })
                    homecare.Status = HomecareServiceStatus.Confirmed;
                break;
        }

        await _db.SaveChangesAsync();
    }

    // ============ QR ============

    public string? RenderQrImage(string? payload, int pixelsPerModule = 8)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;

        // Penyedia tertentu mengembalikan URL gambar, bukan payload mentah.
        if (payload.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            payload.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return payload;

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(data).GetGraphic(pixelsPerModule);
            return "data:image/png;base64," + Convert.ToBase64String(png);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[Payment] Gagal membuat gambar QR");
            return null;
        }
    }

    public (bool Ok, string Message) ValidateQrisConfig()
    {
        if (string.IsNullOrWhiteSpace(_cfg.Qris?.StaticPayload))
            return (false, "Payload QRIS merchant belum diisi.");

        var (ok, message) = QrisPayload.Validate(_cfg.Qris.StaticPayload);
        if (!ok) return (false, message);

        try
        {
            var sample = QrisPayload.WithAmount(_cfg.Qris.StaticPayload, 10000);
            var check = QrisPayload.Validate(sample);
            return check.Ok
                ? (true, message + " Uji pembuatan QR dinamis berhasil.")
                : (false, "QR dinamis gagal dibentuk: " + check.Message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
