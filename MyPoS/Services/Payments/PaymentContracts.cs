using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyPoS.Services.Payments
{
    public record PaymentItem(string Name, int Quantity, decimal Price);

    public record PaymentRequest
    {
        public required string InvoiceNumber { get; init; }
        public required decimal Amount { get; init; }
        public string CurrencyCode { get; init; } = "IDR";
        public string? CustomerName { get; init; }
        public string? CustomerEmail { get; init; }
        public string? CustomerPhone { get; init; }
        public string? SuccessUrl { get; init; }
        public string? FailureUrl { get; init; }
        public IReadOnlyList<PaymentItem> Items { get; init; } = new List<PaymentItem>();
    }

    /// <summary>Status yang sudah diseragamkan dari beragam istilah tiap gateway.</summary>
    public enum PaymentState
    {
        Pending,
        Paid,
        Failed,
        Expired,
        Unknown
    }

    public record PaymentResult
    {
        public required bool Success { get; init; }
        public required string Provider { get; init; }
        public PaymentState State { get; init; } = PaymentState.Unknown;
        /// <summary>ID transaksi di sisi gateway, disimpan untuk rekonsiliasi.</summary>
        public string? Reference { get; init; }
        /// <summary>Halaman pembayaran yang dibuka pelanggan.</summary>
        public string? RedirectUrl { get; init; }
        public string? Message { get; init; }

        public static PaymentResult Fail(string provider, string message) =>
            new() { Success = false, Provider = provider, State = PaymentState.Failed, Message = message };
    }

    /// <summary>
    /// Kontrak seragam untuk semua penyedia pembayaran. Menambah penyedia baru cukup dengan
    /// mengimplementasikan antarmuka ini lalu mendaftarkannya di <see cref="PaymentGatewayResolver"/>.
    /// </summary>
    public interface IPaymentGateway
    {
        /// <summary>Nama teknis, dipakai sebagai nilai kolom PaymentProvider.</summary>
        string Name { get; }
        /// <summary>Label yang dilihat kasir.</summary>
        string DisplayName { get; }
        /// <summary>Ikon Material yang mewakili penyedia ini.</summary>
        string Icon { get; }
        /// <summary>true bila penyedia diaktifkan dan kredensialnya sudah diisi.</summary>
        bool IsConfigured(PosSettings settings);
        /// <summary>false untuk tunai: pembayaran selesai di tempat tanpa membuka halaman gateway.</summary>
        bool RequiresRedirect { get; }

        Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, PosSettings settings, CancellationToken ct = default);
        Task<PaymentResult> CheckStatusAsync(string reference, PosSettings settings, CancellationToken ct = default);
    }
}
