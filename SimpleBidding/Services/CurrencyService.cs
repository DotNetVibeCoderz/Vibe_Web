using System.Globalization;

namespace SimpleBidding.Services
{
    public class CurrencyService
    {
        private readonly string _symbol;
        private readonly CultureInfo _culture;

        public CurrencyService(IConfiguration config)
        {
            _symbol = config["CurrencySymbol"] ?? "Rp";
            var cultureName = config["CurrencyCulture"] ?? "id-ID";
            _culture = new CultureInfo(cultureName);
        }

        public string Format(decimal amount)
        {
            // Manual format to ensure the symbol from config is used
            return $"{_symbol} {amount.ToString("N0", _culture)}";
        }

        public string Symbol => _symbol;
    }
}
