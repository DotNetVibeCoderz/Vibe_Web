using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VirtualDoctor.Data;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services.Payment;

/// <summary>Jawaban untuk penyedia beserta hasil pemrosesannya.</summary>
public record WebhookResult(int StatusCode, string Message, WebhookOutcome Outcome);

public interface IPaymentWebhookService
{
    /// <summary>Terima satu kiriman webhook: periksa keasliannya, catat, lalu terapkan.</summary>
    Task<WebhookResult> ReceiveAsync(string provider, string body, string? callbackToken, CancellationToken ct = default);

    Task<List<PaymentWebhookEvent>> RecentAsync(int limit = 200);

    /// <summary>Proses ulang kiriman yang sudah tercatat, mis. setelah gangguan database.</summary>
    Task<WebhookResult> ReplayAsync(string eventId, string actor, CancellationToken ct = default);
}

/// <summary>
/// Gerbang tunggal untuk seluruh webhook penyedia pembayaran.
///
/// Pemeriksaan tanda tangan, pemetaan status, dan penerapannya diletakkan di sini —
/// bukan di endpoint minimal API — karena tiga alasan: kiriman dapat dicatat apa pun
/// hasilnya, pengiriman ulang dapat dikenali dari sidik jari isinya, dan petugas dapat
/// menjalankan ulang isi yang sama tanpa meminta penyedia mengirim lagi.
/// </summary>
public class PaymentWebhookService : IPaymentWebhookService
{
    /// <summary>Notifikasi yang jauh lebih tua dari ini dianggap kedaluwarsa.</summary>
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);

    /// <summary>Batas isi yang disimpan, supaya tabel jejak tidak membengkak.</summary>
    private const int MaxPayloadLength = 8000;

    private readonly AppDbContext _db;
    private readonly IPaymentService _payments;
    private readonly PaymentConfig _cfg;
    private readonly ILogger<PaymentWebhookService> _log;

    public PaymentWebhookService(AppDbContext db, IPaymentService payments, AppConfig config, ILogger<PaymentWebhookService> log)
    {
        _db = db;
        _payments = payments;
        _cfg = config.Payment;
        _log = log;
    }

    // ============ Penerimaan ============

    public async Task<WebhookResult> ReceiveAsync(string provider, string body, string? callbackToken, CancellationToken ct = default)
    {
        var fingerprint = Fingerprint(provider, body);

        // Kiriman ulang dikenali dari isi yang identik. Yang sudah pernah sampai pada
        // keputusan cukup dihitung ulang jumlah kirimannya; hanya yang ditolak atau
        // gagal yang boleh diproses lagi lewat baris yang sama.
        var record = await _db.PaymentWebhookEvents.FirstOrDefaultAsync(e => e.Fingerprint == fingerprint, ct);
        if (record != null)
        {
            record.Attempts++;
            if (record.Outcome is WebhookOutcome.Processed or WebhookOutcome.Duplicate or WebhookOutcome.Ignored)
            {
                await _db.SaveChangesAsync(ct);
                _log.LogInformation("[Webhook] Kiriman {Provider} berulang untuk {Inv} (ke-{N}), status tidak diubah",
                    provider, record.InvoiceNumber, record.Attempts);
                return new WebhookResult(200, "Kiriman ulang, status tidak diubah.", WebhookOutcome.Duplicate);
            }
        }
        else
        {
            record = new PaymentWebhookEvent
            {
                Provider = provider,
                Fingerprint = fingerprint,
                Payload = Truncate(body),
                ReceivedAt = DateTime.UtcNow
            };
            _db.PaymentWebhookEvents.Add(record);
        }

        var result = await ProcessAsync(record, body, callbackToken, ct);
        await _db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<List<PaymentWebhookEvent>> RecentAsync(int limit = 200) =>
        await _db.PaymentWebhookEvents.AsNoTracking()
            .OrderByDescending(e => e.ReceivedAt)
            .Take(limit)
            .ToListAsync();

    public async Task<WebhookResult> ReplayAsync(string eventId, string actor, CancellationToken ct = default)
    {
        var record = await _db.PaymentWebhookEvents.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (record == null) return new WebhookResult(404, "Jejak webhook tidak ditemukan.", WebhookOutcome.Failed);

        // Kiriman yang tanda tangannya tidak sah tidak boleh dijalankan ulang oleh siapa pun:
        // kalau boleh, tombol ini menjadi jalan memutar pemeriksaan keaslian.
        if (record.Outcome == WebhookOutcome.Rejected)
            return new WebhookResult(403, "Kiriman ini gagal pemeriksaan keaslian dan tidak dapat diproses ulang.", WebhookOutcome.Rejected);

        if (record.Payload.Length >= MaxPayloadLength)
            return new WebhookResult(400, "Isi kiriman tersimpan terpotong sehingga tidak dapat diproses ulang.", WebhookOutcome.Failed);

        record.ReplayedBy = actor;
        record.ReplayedAt = DateTime.UtcNow;

        // Token callback tidak ikut tersimpan; baris ini sudah lolos pemeriksaan
        // saat pertama diterima, jadi pemeriksaan token dilewati di sini.
        var result = await ProcessAsync(record, record.Payload, _cfg.Xendit?.CallbackToken, ct);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("[Webhook] {Actor} memproses ulang {Id}: {Hasil}", actor, eventId, result.Message);
        return result;
    }

    // ============ Inti pemrosesan ============

    private async Task<WebhookResult> ProcessAsync(PaymentWebhookEvent record, string body, string? callbackToken, CancellationToken ct)
    {
        Parsed parsed;
        try
        {
            parsed = record.Provider.Equals("Xendit", StringComparison.OrdinalIgnoreCase)
                ? ParseXendit(body, callbackToken)
                : ParseMidtrans(body);
        }
        catch (JsonException)
        {
            return Finish(record, WebhookOutcome.Failed, "Isi kiriman bukan JSON yang sah.", 400);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[Webhook] {Provider} gagal dibaca", record.Provider);
            return Finish(record, WebhookOutcome.Failed, "Isi kiriman tidak dapat dibaca.", 400);
        }

        record.InvoiceNumber = parsed.InvoiceNumber;
        record.ExternalId = parsed.ExternalId;
        record.RawStatus = parsed.RawStatus;
        record.MappedState = parsed.State;
        record.SignatureValid = parsed.SignatureValid;

        if (!parsed.SignatureValid)
        {
            _log.LogWarning("[Webhook] Keaslian {Provider} tidak terbukti untuk {Inv}: {Alasan}",
                record.Provider, parsed.InvoiceNumber, parsed.Error);
            return Finish(record, WebhookOutcome.Rejected, parsed.Error ?? "Tanda tangan tidak cocok.", 401);
        }

        if (string.IsNullOrWhiteSpace(parsed.InvoiceNumber))
            return Finish(record, WebhookOutcome.Failed, "Nomor tagihan tidak ada di dalam kiriman.", 400);

        if (parsed.State == null)
            return Finish(record, WebhookOutcome.Ignored, $"Status \"{parsed.RawStatus}\" tidak dipetakan, diabaikan.", 200);

        // Notifikasi yang sangat terlambat tidak boleh mengubah pembukuan yang sudah ditutup.
        if (parsed.EventTime != null && DateTime.UtcNow - parsed.EventTime.Value > MaxAge)
            return Finish(record, WebhookOutcome.Ignored,
                $"Notifikasi tertanggal {parsed.EventTime:dd MMM yyyy} sudah kedaluwarsa, diabaikan.", 200);

        try
        {
            var applied = await _payments.ApplyExternalStatusAsync(parsed.InvoiceNumber, parsed.State.Value, parsed.ExternalId);

            if (!applied.Found) return Finish(record, WebhookOutcome.Failed, applied.Message, 404);
            return applied.Changed
                ? Finish(record, WebhookOutcome.Processed, applied.Message, 200)
                : Finish(record, WebhookOutcome.Ignored, applied.Message, 200);
        }
        catch (Exception ex)
        {
            // Dibiarkan sebagai Failed supaya terlihat di UI admin dan bisa dijalankan ulang.
            _log.LogError(ex, "[Webhook] Gagal menerapkan status {Inv}", parsed.InvoiceNumber);
            return Finish(record, WebhookOutcome.Failed, "Gagal menerapkan status: " + ex.Message, 500);
        }
    }

    private static WebhookResult Finish(PaymentWebhookEvent record, WebhookOutcome outcome, string message, int status)
    {
        record.Outcome = outcome;
        record.Message = message;
        record.ProcessedAt = DateTime.UtcNow;
        return new WebhookResult(status, message, outcome);
    }

    // ============ Pembacaan per penyedia ============

    private record Parsed(
        string? InvoiceNumber, string? ExternalId, string? RawStatus,
        PaymentState? State, bool SignatureValid, string? Error, DateTime? EventTime);

    /// <summary>
    /// Midtrans menandatangani notifikasinya dengan SHA-512 atas gabungan
    /// order_id + status_code + gross_amount + server key.
    /// </summary>
    private Parsed ParseMidtrans(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        string? Read(string name) => root.TryGetProperty(name, out var v) ? v.ToString() : null;

        var orderId = Read("order_id");
        var signature = Read("signature_key");
        var serverKey = _cfg.Midtrans?.ServerKey ?? "";
        var rawStatus = Read("transaction_status");
        var externalId = Read("transaction_id");
        var eventTime = ParseTime(Read("transaction_time"));

        if (string.IsNullOrEmpty(serverKey))
            return new Parsed(orderId, externalId, rawStatus, null, false, "Server key Midtrans belum diisi.", eventTime);

        var expected = Convert.ToHexString(SHA512.HashData(
            Encoding.UTF8.GetBytes(orderId + Read("status_code") + Read("gross_amount") + serverKey))).ToLowerInvariant();

        var valid = !string.IsNullOrEmpty(signature) &&
                    CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(expected),
                        Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));

        var state = rawStatus switch
        {
            "settlement" or "capture" => PaymentState.Paid,
            "pending" => PaymentState.Pending,
            "expire" => PaymentState.Expired,
            "deny" or "cancel" or "failure" => PaymentState.Failed,
            "refund" or "partial_refund" => PaymentState.Refunded,
            _ => (PaymentState?)null
        };

        return new Parsed(orderId, externalId, rawStatus, state, valid,
            valid ? null : "Tanda tangan Midtrans tidak cocok.", eventTime);
    }

    /// <summary>Xendit mengirim token callback lewat header, bukan tanda tangan isi.</summary>
    private Parsed ParseXendit(string body, string? callbackToken)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        string? Read(string name) => root.TryGetProperty(name, out var v) ? v.ToString() : null;

        var invoice = Read("external_id") ?? Read("reference_id");
        var rawStatus = Read("status");
        var externalId = Read("id");
        var eventTime = ParseTime(Read("updated") ?? Read("created") ?? Read("paid_at"));
        var expected = _cfg.Xendit?.CallbackToken;

        if (string.IsNullOrEmpty(expected))
            return new Parsed(invoice, externalId, rawStatus, null, false, "Token callback Xendit belum diisi.", eventTime);

        var valid = !string.IsNullOrEmpty(callbackToken) &&
                    CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(expected),
                        Encoding.UTF8.GetBytes(callbackToken));

        var state = rawStatus switch
        {
            "PAID" or "SETTLED" or "COMPLETED" or "SUCCEEDED" => PaymentState.Paid,
            "EXPIRED" => PaymentState.Expired,
            "FAILED" or "INACTIVE" => PaymentState.Failed,
            "REFUNDED" => PaymentState.Refunded,
            _ => (PaymentState?)null
        };

        return new Parsed(invoice, externalId, rawStatus, state, valid,
            valid ? null : "Token callback Xendit tidak cocok.", eventTime);
    }

    // ============ Pembantu ============

    private static DateTime? ParseTime(string? value) =>
        DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var parsed) ? parsed : null;

    private static string Fingerprint(string provider, string body) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(provider + "|" + body))).ToLowerInvariant();

    private static string Truncate(string body) =>
        body.Length <= MaxPayloadLength ? body : body[..MaxPayloadLength];
}
