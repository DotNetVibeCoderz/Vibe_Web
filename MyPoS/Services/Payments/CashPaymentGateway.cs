using System.Threading;
using System.Threading.Tasks;

namespace MyPoS.Services.Payments
{
    /// <summary>Pembayaran tunai di konter: langsung lunas, tidak memanggil layanan luar.</summary>
    public class CashPaymentGateway : IPaymentGateway
    {
        public string Name => "Cash";
        public string DisplayName => "Tunai";
        public string Icon => MudBlazor.Icons.Material.Filled.Payments;
        public bool RequiresRedirect => false;

        public bool IsConfigured(PosSettings settings) => settings.PaymentCashEnabled;

        public Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, PosSettings settings, CancellationToken ct = default)
            => Task.FromResult(new PaymentResult
            {
                Success = true,
                Provider = Name,
                State = PaymentState.Paid,
                Reference = request.InvoiceNumber,
                Message = "Pembayaran tunai diterima"
            });

        public Task<PaymentResult> CheckStatusAsync(string reference, PosSettings settings, CancellationToken ct = default)
            => Task.FromResult(new PaymentResult
            {
                Success = true,
                Provider = Name,
                State = PaymentState.Paid,
                Reference = reference
            });
    }
}
