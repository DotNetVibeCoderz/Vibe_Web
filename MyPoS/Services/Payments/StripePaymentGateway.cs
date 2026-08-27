using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyPoS.Services.Payments
{
    /// <summary>
    /// Stripe Checkout Session (form-encoded, bukan JSON). Otentikasi Bearer memakai Secret Key.
    /// </summary>
    public class StripePaymentGateway : IPaymentGateway
    {
        private const string BaseUrl = "https://api.stripe.com";

        /// <summary>
        /// Mata uang tanpa satuan pecahan; nominalnya dikirim apa adanya. Selain yang ada di
        /// daftar ini - termasuk IDR - nominal dikirim dalam satuan terkecil (dikali 100).
        /// </summary>
        private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA",
            "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StripePaymentGateway> _logger;

        public StripePaymentGateway(IHttpClientFactory httpClientFactory, ILogger<StripePaymentGateway> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string Name => "Stripe";
        public string DisplayName => "Stripe";
        public string Icon => MudBlazor.Icons.Material.Filled.CreditCard;
        public bool RequiresRedirect => true;

        public bool IsConfigured(PosSettings settings)
            => settings.StripeEnabled && !string.IsNullOrWhiteSpace(settings.StripeSecretKey);

        public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, PosSettings settings, CancellationToken ct = default)
        {
            if (!IsConfigured(settings))
                return PaymentResult.Fail(Name, "Stripe belum diaktifkan atau Secret Key masih kosong.");

            var currency = string.IsNullOrWhiteSpace(settings.StripeCurrency) ? "idr" : settings.StripeCurrency.Trim().ToLowerInvariant();
            var unitAmount = ToMinorUnits(request.Amount, currency);

            if (unitAmount <= 0)
                return PaymentResult.Fail(Name, "Nominal pembayaran harus lebih besar dari nol.");

            // Total Checkout Session selalu sama dengan jumlah line item dan Stripe tidak
            // menerima baris bernilai negatif, sehingga diskon/pajak/pembulatan tidak dapat
            // dikirim sebagai baris tersendiri. Satu baris gabungan menjamin nominal yang
            // ditagih persis sama dengan total transaksi; rinciannya tetap ada di struk.
            var itemSummary = request.Items.Count == 0
                ? "Pembelian"
                : string.Join(", ", request.Items.Take(3).Select(i => $"{i.Quantity}x {i.Name}"))
                  + (request.Items.Count > 3 ? $" +{request.Items.Count - 3} item lain" : "");

            var form = new List<KeyValuePair<string, string>>
            {
                new("mode", "payment"),
                new("client_reference_id", request.InvoiceNumber),
                new("line_items[0][quantity]", "1"),
                new("line_items[0][price_data][currency]", currency),
                new("line_items[0][price_data][unit_amount]", unitAmount.ToString(CultureInfo.InvariantCulture)),
                new("line_items[0][price_data][product_data][name]", $"{settings.StoreName} - {request.InvoiceNumber}"),
                new("line_items[0][price_data][product_data][description]", Truncate(itemSummary, 300)),
                new("metadata[invoice_number]", request.InvoiceNumber)
            };

            if (!string.IsNullOrWhiteSpace(request.SuccessUrl)) form.Add(new("success_url", request.SuccessUrl!));
            if (!string.IsNullOrWhiteSpace(request.FailureUrl)) form.Add(new("cancel_url", request.FailureUrl!));
            if (!string.IsNullOrWhiteSpace(request.CustomerEmail)) form.Add(new("customer_email", request.CustomerEmail!));

            try
            {
                using var client = CreateClient(settings);
                using var response = await client.PostAsync("/v1/checkout/sessions", new FormUrlEncodedContent(form), ct);
                var json = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Stripe menolak sesi {Invoice}: {Status} {Body}", request.InvoiceNumber, response.StatusCode, json);
                    return PaymentResult.Fail(Name, ReadErrorMessage(json, response.StatusCode.ToString()));
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new PaymentResult
                {
                    Success = true,
                    Provider = Name,
                    State = MapState(
                        root.TryGetProperty("payment_status", out var ps) ? ps.GetString() : null,
                        root.TryGetProperty("status", out var st) ? st.GetString() : null),
                    Reference = root.TryGetProperty("id", out var id) ? id.GetString() : null,
                    RedirectUrl = root.TryGetProperty("url", out var url) ? url.GetString() : null,
                    Message = "Sesi Stripe Checkout dibuat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menghubungi Stripe untuk {Invoice}", request.InvoiceNumber);
                return PaymentResult.Fail(Name, $"Tidak dapat menghubungi Stripe: {ex.Message}");
            }
        }

        public async Task<PaymentResult> CheckStatusAsync(string reference, PosSettings settings, CancellationToken ct = default)
        {
            if (!IsConfigured(settings))
                return PaymentResult.Fail(Name, "Stripe belum dikonfigurasi.");

            try
            {
                using var client = CreateClient(settings);
                using var response = await client.GetAsync($"/v1/checkout/sessions/{Uri.EscapeDataString(reference)}", ct);
                var json = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                    return PaymentResult.Fail(Name, ReadErrorMessage(json, response.StatusCode.ToString()));

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new PaymentResult
                {
                    Success = true,
                    Provider = Name,
                    State = MapState(
                        root.TryGetProperty("payment_status", out var ps) ? ps.GetString() : null,
                        root.TryGetProperty("status", out var st) ? st.GetString() : null),
                    Reference = reference
                };
            }
            catch (Exception ex)
            {
                return PaymentResult.Fail(Name, $"Tidak dapat menghubungi Stripe: {ex.Message}");
            }
        }

        private HttpClient CreateClient(PosSettings settings)
        {
            var client = _httpClientFactory.CreateClient(nameof(StripePaymentGateway));
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.StripeSecretKey.Trim());
            return client;
        }

        internal static long ToMinorUnits(decimal amount, string currency)
        {
            var factor = ZeroDecimalCurrencies.Contains(currency) ? 1m : 100m;
            return (long)Math.Round(amount * factor, 0, MidpointRounding.AwayFromZero);
        }

        internal static PaymentState MapState(string? paymentStatus, string? sessionStatus)
        {
            if (string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase)) return PaymentState.Paid;
            return sessionStatus?.ToLowerInvariant() switch
            {
                "open" => PaymentState.Pending,
                "expired" => PaymentState.Expired,
                "complete" => PaymentState.Paid,
                _ => PaymentState.Pending
            };
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value[..max];

        private static string ReadErrorMessage(string json, string fallback)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var err) && err.TryGetProperty("message", out var m))
                    return m.GetString() ?? fallback;
            }
            catch (JsonException)
            {
                // biarkan memakai fallback
            }
            return fallback;
        }
    }
}
