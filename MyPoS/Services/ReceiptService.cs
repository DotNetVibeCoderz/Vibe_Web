using System;
using System.Linq;
using System.Text;
using MyPoS.Models;

namespace MyPoS.Services
{
    /// <summary>
    /// Menyusun struk dalam teks monospace, siap dikirim ke printer termal 58/80 mm
    /// atau disalin sebagai bukti transaksi. Lebar kertas dan isi footer diambil dari
    /// halaman Pengaturan, bukan ditanam di kode.
    /// </summary>
    public class ReceiptService
    {
        private readonly SettingsService _settings;
        private readonly MoneyFormatter _money;

        public ReceiptService(SettingsService settings, MoneyFormatter money)
        {
            _settings = settings;
            _money = money;
        }

        /// <summary>58 mm memuat sekitar 32 karakter, 80 mm sekitar 48 karakter.</summary>
        public int Columns => _settings.Current.ReceiptPaperWidthMm <= 58 ? 32 : 48;

        public string BuildText(Transaction transaction)
        {
            var s = _settings.Current;
            var width = Columns;
            var sb = new StringBuilder();

            void Center(string text) => sb.AppendLine(text.Length >= width ? text[..width] : text.PadLeft((width + text.Length) / 2));
            void Rule(char c = '-') => sb.AppendLine(new string(c, width));
            void Row(string left, string right)
            {
                var space = width - left.Length - right.Length;
                if (space < 1)
                {
                    left = left[..Math.Max(0, width - right.Length - 1)];
                    space = 1;
                }
                sb.AppendLine(left + new string(' ', space) + right);
            }

            Center(s.StoreName.ToUpperInvariant());
            if (!string.IsNullOrWhiteSpace(s.StoreAddress)) Center(s.StoreAddress);
            if (!string.IsNullOrWhiteSpace(s.StorePhone)) Center("Telp. " + s.StorePhone);
            if (!string.IsNullOrWhiteSpace(s.StoreTaxId)) Center("NPWP " + s.StoreTaxId);
            Rule('=');

            Row("No.", transaction.InvoiceNumber);
            Row("Tanggal", transaction.Date.ToString("dd/MM/yyyy HH:mm"));
            if (s.ReceiptShowCashier) Row("Kasir", transaction.CashierName);
            if (transaction.Customer is not null) Row("Pelanggan", transaction.Customer.Name);
            Rule();

            foreach (var detail in transaction.Details)
            {
                sb.AppendLine(detail.ProductName);
                Row($"  {detail.Quantity} x {_money.FormatNumber(detail.UnitPrice)}", _money.FormatNumber(detail.SubTotal));
                if (detail.DiscountAmount > 0)
                    Row("  Diskon", "-" + _money.FormatNumber(detail.DiscountAmount));
            }

            Rule();
            Row("Subtotal", _money.FormatNumber(transaction.SubTotal));

            if (transaction.DiscountAmount > 0)
                Row("Diskon", "-" + _money.FormatNumber(transaction.DiscountAmount));

            if (transaction.ServiceChargeAmount > 0)
                Row("Layanan", _money.FormatNumber(transaction.ServiceChargeAmount));

            if (transaction.TaxAmount > 0)
            {
                var label = $"{s.TaxName} {transaction.TaxRate:0.##}%" + (transaction.TaxInclusive ? " (termasuk)" : "");
                Row(label, _money.FormatNumber(transaction.TaxAmount));
            }

            if (transaction.RoundingAmount != 0)
                Row("Pembulatan", _money.FormatNumber(transaction.RoundingAmount));

            Rule('=');
            Row("TOTAL", _money.Format(transaction.TotalAmount));
            Row(transaction.PaymentMethod, _money.FormatNumber(transaction.PaidAmount));

            if (transaction.ChangeAmount > 0)
                Row("Kembali", _money.FormatNumber(transaction.ChangeAmount));

            Rule();
            var itemCount = transaction.Details.Sum(d => d.Quantity);
            Center($"{transaction.Details.Count} jenis / {itemCount} barang");

            if (transaction.Status != TransactionStatus.Paid)
                Center($"** {StatusLabel(transaction.Status).ToUpperInvariant()} **");

            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(s.ReceiptFooter)) Center(s.ReceiptFooter);

            return sb.ToString();
        }

        public static string StatusLabel(TransactionStatus status) => status switch
        {
            TransactionStatus.Paid => "Lunas",
            TransactionStatus.Pending => "Menunggu pembayaran",
            TransactionStatus.Failed => "Gagal",
            TransactionStatus.Voided => "Dibatalkan",
            TransactionStatus.Refunded => "Dikembalikan",
            _ => status.ToString()
        };

        public static MudBlazor.Color StatusColor(TransactionStatus status) => status switch
        {
            TransactionStatus.Paid => MudBlazor.Color.Success,
            TransactionStatus.Pending => MudBlazor.Color.Warning,
            TransactionStatus.Failed or TransactionStatus.Voided => MudBlazor.Color.Error,
            TransactionStatus.Refunded => MudBlazor.Color.Info,
            _ => MudBlazor.Color.Default
        };
    }
}
