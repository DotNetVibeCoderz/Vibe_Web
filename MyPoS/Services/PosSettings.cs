namespace MyPoS.Services
{
    /// <summary>
    /// Seluruh nilai yang dulu di-hardcode di dalam halaman sekarang tinggal di sini.
    /// Setiap properti disimpan sebagai satu baris <see cref="MyPoS.Models.AppSetting"/>
    /// dengan Key = nama properti, sehingga menambah pengaturan baru tidak mengubah skema DB.
    /// </summary>
    public class PosSettings
    {
        // ---------- Identitas toko ----------
        public string StoreName { get; set; } = "MyPoS";
        public string StoreTagline { get; set; } = "Kasir Toko Modern";
        public string StoreAddress { get; set; } = "Jl. Merdeka No. 1, Jakarta Pusat";
        public string StorePhone { get; set; } = "021-1234567";
        public string StoreTaxId { get; set; } = "";
        public string StoreLogoUrl { get; set; } = "";

        // ---------- Mata uang ----------
        public string CurrencyCode { get; set; } = "IDR";
        public string CurrencySymbol { get; set; } = "Rp";
        public string CurrencyCulture { get; set; } = "id-ID";
        /// <summary>Jumlah angka di belakang koma. Rupiah lazimnya 0.</summary>
        public int CurrencyDecimals { get; set; } = 0;
        /// <summary>prefix = "Rp 15.000", suffix = "15.000 Rp".</summary>
        public string CurrencySymbolPosition { get; set; } = "prefix";

        // ---------- Pajak dan biaya ----------
        public bool TaxEnabled { get; set; } = true;
        public string TaxName { get; set; } = "PPN";
        public decimal TaxRatePercent { get; set; } = 11m;
        /// <summary>true = harga jual sudah termasuk pajak (pajak diurai dari harga).</summary>
        public bool TaxInclusive { get; set; } = false;
        /// <summary>true = pajak dihitung setelah diskon (DPP = subtotal - diskon).</summary>
        public bool TaxAppliedAfterDiscount { get; set; } = true;
        public bool ServiceChargeEnabled { get; set; } = false;
        public decimal ServiceChargePercent { get; set; } = 5m;
        /// <summary>true = service charge ikut menjadi dasar pengenaan pajak.</summary>
        public bool ServiceChargeTaxable { get; set; } = true;
        /// <summary>None | Nearest100 | Nearest500 | Nearest1000</summary>
        public string RoundingMode { get; set; } = "None";

        // ---------- Struk ----------
        public string InvoicePrefix { get; set; } = "INV";
        public string ReceiptFooter { get; set; } = "Terima kasih atas kunjungan Anda";
        public bool ReceiptShowLogo { get; set; } = true;
        public bool ReceiptShowCashier { get; set; } = true;
        public int ReceiptPaperWidthMm { get; set; } = 80;

        // ---------- Stok ----------
        public int LowStockThreshold { get; set; } = 10;
        public bool BlockSaleWhenOutOfStock { get; set; } = true;

        // ---------- Loyalitas ----------
        public bool LoyaltyEnabled { get; set; } = true;
        /// <summary>Nominal belanja yang setara dengan 1 poin. 10000 = 1 poin tiap Rp 10.000.</summary>
        public decimal LoyaltyAmountPerPoint { get; set; } = 10000m;

        // ---------- Sesi ----------
        /// <summary>Berapa lama kasir tetap masuk sebelum diminta login ulang.</summary>
        public int SessionTimeoutHours { get; set; } = 12;

        // ---------- Tampilan ----------
        public bool DefaultDarkMode { get; set; } = false;
        public string AccentColor { get; set; } = "#B3382B";

        // ---------- Pembayaran ----------
        public bool PaymentCashEnabled { get; set; } = true;
        /// <summary>Base URL publik untuk redirect/callback gateway, mis. https://toko.example.com</summary>
        public string PublicBaseUrl { get; set; } = "";

        public bool XenditEnabled { get; set; } = false;
        public string XenditSecretKey { get; set; } = "";
        public string XenditWebhookToken { get; set; } = "";

        public bool MidtransEnabled { get; set; } = false;
        public string MidtransServerKey { get; set; } = "";
        public string MidtransClientKey { get; set; } = "";
        public bool MidtransIsProduction { get; set; } = false;

        public bool StripeEnabled { get; set; } = false;
        public string StripeSecretKey { get; set; } = "";
        /// <summary>Kode mata uang yang dikirim ke Stripe (huruf kecil), mis. idr / usd.</summary>
        public string StripeCurrency { get; set; } = "idr";

        public PosSettings Clone() => (PosSettings)MemberwiseClone();
    }
}
