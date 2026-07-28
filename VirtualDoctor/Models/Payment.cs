namespace VirtualDoctor.Models;

/// <summary>Jenis transaksi yang dibayar.</summary>
public enum PaymentReferenceType { Order, Consultation, Appointment, Homecare }

/// <summary>Status siklus hidup pembayaran.</summary>
public enum PaymentState
{
    /// <summary>Menunggu pembayaran dari pasien.</summary>
    Pending,
    /// <summary>Bukti sudah diunggah, menunggu diperiksa petugas.</summary>
    AwaitingVerification,
    /// <summary>Dana diterima.</summary>
    Paid,
    /// <summary>Melewati batas waktu.</summary>
    Expired,
    /// <summary>Ditolak saat verifikasi atau gagal di penyedia.</summary>
    Failed,
    /// <summary>Dana dikembalikan.</summary>
    Refunded
}

/// <summary>Cara pembayaran yang dipilih pasien.</summary>
public enum PaymentChannel { Qris, BankTransfer, VirtualAccount, EWallet, Card, Cash, Insurance }

/// <summary>
/// Satu tagihan beserta jejak pembayarannya. Dibuat untuk setiap transaksi
/// yang perlu dibayar: pesanan obat, konsultasi, janji temu, atau homecare.
/// </summary>
public class Payment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Nomor tagihan yang tampil di invoice, mis. INV/2026/07/0001.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    public PaymentReferenceType ReferenceType { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }        // nilai layanan
    public decimal ServiceFee { get; set; }    // biaya penanganan penyedia
    public decimal Discount { get; set; }
    public decimal InsuranceCoverage { get; set; }
    public decimal Total { get; set; }         // yang harus dibayar pasien

    public PaymentChannel Channel { get; set; } = PaymentChannel.Qris;
    public string Provider { get; set; } = "Manual";
    public PaymentState State { get; set; } = PaymentState.Pending;

    // Data dari penyedia pembayaran
    public string? ExternalId { get; set; }      // id transaksi di sisi penyedia
    public string? QrPayload { get; set; }       // string QRIS mentah
    public string? PaymentUrl { get; set; }      // halaman bayar penyedia
    public string? VirtualAccountNumber { get; set; }

    // Bukti manual
    public string? ProofUrl { get; set; }
    public string? PayerNote { get; set; }

    // Verifikasi
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public bool IsSettled => State == PaymentState.Paid;
    public bool IsOutstanding => State is PaymentState.Pending or PaymentState.AwaitingVerification;
}

/// <summary>
/// Penomoran invoice per bulan. Satu baris untuk setiap awalan, mis. <c>INV/2026/07/</c>.
/// Nomor dinaikkan lewat satu pernyataan UPDATE di dalam transaksi sehingga dua
/// permintaan bersamaan tidak pernah menerima nomor yang sama.
/// </summary>
public class InvoiceCounter
{
    public string Prefix { get; set; } = string.Empty;
    public int LastNumber { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Hasil pemrosesan satu kiriman webhook.</summary>
public enum WebhookOutcome
{
    /// <summary>Status tagihan benar-benar berubah karena kiriman ini.</summary>
    Processed,
    /// <summary>Isi yang sama sudah pernah diproses, jadi diabaikan.</summary>
    Duplicate,
    /// <summary>Sah, tetapi tidak menyebabkan perubahan (status sama atau mundur).</summary>
    Ignored,
    /// <summary>Tanda tangan atau token tidak cocok.</summary>
    Rejected,
    /// <summary>Isi tidak dapat dibaca atau pemrosesan gagal.</summary>
    Failed
}

/// <summary>
/// Jejak setiap kiriman webhook penyedia pembayaran. Disimpan apa pun hasilnya —
/// termasuk yang ditolak — supaya masalah integrasi terlihat di UI admin dan
/// kiriman yang gagal dapat diproses ulang tanpa meminta penyedia mengirim lagi.
/// </summary>
public class PaymentWebhookEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Provider { get; set; } = string.Empty;   // Midtrans | Xendit
    public string? InvoiceNumber { get; set; }
    public string? ExternalId { get; set; }

    /// <summary>Status mentah dari penyedia, mis. <c>settlement</c> atau <c>PAID</c>.</summary>
    public string? RawStatus { get; set; }
    public PaymentState? MappedState { get; set; }

    /// <summary>SHA-256 penyedia + isi kiriman. Dipakai mengenali pengiriman ulang.</summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>Isi mentah kiriman, dipotong agar tabel tidak membengkak.</summary>
    public string Payload { get; set; } = string.Empty;

    public bool SignatureValid { get; set; }
    public WebhookOutcome Outcome { get; set; }
    public string? Message { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Berapa kali isi ini diterima, termasuk pengiriman ulang penyedia.</summary>
    public int Attempts { get; set; } = 1;

    public string? ReplayedBy { get; set; }
    public DateTime? ReplayedAt { get; set; }
}

public static class PaymentLabels
{
    public static string State(PaymentState s) => s switch
    {
        PaymentState.Pending => "Menunggu pembayaran",
        PaymentState.AwaitingVerification => "Menunggu verifikasi",
        PaymentState.Paid => "Lunas",
        PaymentState.Expired => "Kedaluwarsa",
        PaymentState.Failed => "Gagal",
        _ => "Dikembalikan"
    };

    public static string StatePill(PaymentState s) => s switch
    {
        PaymentState.Paid => "vd-pill-success",
        PaymentState.Failed or PaymentState.Expired => "vd-pill-danger",
        PaymentState.AwaitingVerification => "vd-pill-info",
        PaymentState.Refunded => "vd-pill-neutral",
        _ => "vd-pill-warning"
    };

    public static string Channel(PaymentChannel c) => c switch
    {
        PaymentChannel.Qris => "QRIS",
        PaymentChannel.BankTransfer => "Transfer bank",
        PaymentChannel.VirtualAccount => "Virtual account",
        PaymentChannel.EWallet => "Dompet digital",
        PaymentChannel.Card => "Kartu",
        PaymentChannel.Cash => "Tunai",
        _ => "Asuransi"
    };

    public static string Reference(PaymentReferenceType t) => t switch
    {
        PaymentReferenceType.Order => "Pesanan obat",
        PaymentReferenceType.Consultation => "Konsultasi",
        PaymentReferenceType.Appointment => "Janji temu",
        _ => "Homecare"
    };

    public static string Outcome(WebhookOutcome o) => o switch
    {
        WebhookOutcome.Processed => "Diproses",
        WebhookOutcome.Duplicate => "Kiriman ulang",
        WebhookOutcome.Ignored => "Diabaikan",
        WebhookOutcome.Rejected => "Ditolak",
        _ => "Gagal"
    };

    public static string OutcomePill(WebhookOutcome o) => o switch
    {
        WebhookOutcome.Processed => "vd-pill-success",
        WebhookOutcome.Duplicate => "vd-pill-neutral",
        WebhookOutcome.Ignored => "vd-pill-info",
        _ => "vd-pill-danger"
    };
}
