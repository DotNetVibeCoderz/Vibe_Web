using System;
using System.Globalization;

namespace MyPoS.Services
{
    /// <summary>
    /// Memformat nominal memakai <see cref="PosSettings"/> alih-alih <c>ToString("C0")</c>,
    /// yang dulu bergantung pada culture sistem server sehingga bisa tampil sebagai dolar
    /// di mesin yang locale-nya bukan Indonesia.
    /// </summary>
    public class MoneyFormatter
    {
        private readonly SettingsService _settings;

        public MoneyFormatter(SettingsService settings)
        {
            _settings = settings;
        }

        public PosSettings Settings => _settings.Current;

        /// <summary>Nominal lengkap dengan simbol, mis. "Rp 15.000".</summary>
        public string Format(decimal amount) => Format(amount, Settings);

        /// <summary>Nominal tanpa simbol, mis. "15.000". Dipakai di kolom tabel yang sudah berjudul.</summary>
        public string FormatNumber(decimal amount) => FormatNumber(amount, Settings);

        /// <summary>Bentuk ringkas untuk kartu statistik, mis. "Rp 1,2 jt".</summary>
        public string FormatCompact(decimal amount)
        {
            var s = Settings;
            var abs = Math.Abs(amount);
            var (value, suffix) = abs switch
            {
                >= 1_000_000_000m => (amount / 1_000_000_000m, " M"),
                >= 1_000_000m => (amount / 1_000_000m, " jt"),
                >= 100_000m => (amount / 1_000m, " rb"),
                _ => (amount, "")
            };

            if (suffix.Length == 0) return Format(amount);

            var culture = ResolveCulture(s);
            return Prefix(s, value.ToString("0.#", culture) + suffix);
        }

        public static string Format(decimal amount, PosSettings s)
            => Prefix(s, FormatNumber(amount, s));

        public static string FormatNumber(decimal amount, PosSettings s)
        {
            var culture = ResolveCulture(s);
            var decimals = Math.Clamp(s.CurrencyDecimals, 0, 4);
            return Math.Round(amount, decimals, MidpointRounding.AwayFromZero)
                       .ToString("N" + decimals, culture);
        }

        private static string Prefix(PosSettings s, string number)
            => string.Equals(s.CurrencySymbolPosition, "suffix", StringComparison.OrdinalIgnoreCase)
                ? $"{number} {s.CurrencySymbol}"
                : $"{s.CurrencySymbol} {number}";

        /// <summary>
        /// Culture dari pengaturan, dengan cadangan pemisah ribuan "." dan desimal ","
        /// supaya format Rupiah tetap benar walau culture id-ID tidak tersedia di host.
        /// </summary>
        private static CultureInfo ResolveCulture(PosSettings s)
        {
            try
            {
                return CultureInfo.GetCultureInfo(s.CurrencyCulture);
            }
            catch (CultureNotFoundException)
            {
                var fallback = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                fallback.NumberFormat.NumberGroupSeparator = ".";
                fallback.NumberFormat.NumberDecimalSeparator = ",";
                return fallback;
            }
        }
    }
}
