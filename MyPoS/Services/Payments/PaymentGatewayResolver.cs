using System;
using System.Collections.Generic;
using System.Linq;

namespace MyPoS.Services.Payments
{
    /// <summary>
    /// Titik tunggal untuk menemukan penyedia pembayaran. Halaman kasir cukup meminta
    /// daftar penyedia yang aktif tanpa tahu implementasinya.
    /// </summary>
    public class PaymentGatewayResolver
    {
        private readonly IReadOnlyList<IPaymentGateway> _gateways;
        private readonly SettingsService _settings;

        public PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways, SettingsService settings)
        {
            _gateways = gateways.ToList();
            _settings = settings;
        }

        /// <summary>Semua penyedia yang dikenal aplikasi, termasuk yang belum dikonfigurasi.</summary>
        public IReadOnlyList<IPaymentGateway> All => _gateways;

        /// <summary>Penyedia yang aktif dan kredensialnya lengkap - inilah yang tampil di kasir.</summary>
        public IReadOnlyList<IPaymentGateway> Enabled(PosSettings? settings = null)
        {
            var s = settings ?? _settings.Current;
            return _gateways.Where(g => g.IsConfigured(s)).ToList();
        }

        public IPaymentGateway? Find(string? name)
            => _gateways.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>Penyedia yang diminta, atau tunai bila namanya tidak dikenali.</summary>
        public IPaymentGateway FindOrCash(string? name)
            => Find(name) ?? _gateways.First(g => g is CashPaymentGateway);
    }
}
