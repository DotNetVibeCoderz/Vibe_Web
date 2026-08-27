using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyPoS.Services.Payments
{
    /// <summary>
    /// Midtrans Snap. Pembuatan transaksi memakai host Snap, sedangkan pengecekan status
    /// memakai host Core API - keduanya berbeda, jadi masing-masing punya base URL sendiri.
    /// Otentikasi HTTP Basic dengan Server Key sebagai username.
    /// </summary>
    public class MidtransPaymentGateway : IPaymentGateway
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MidtransPaymentGateway> _logger;

        public MidtransPaymentGateway(IHttpClientFactory httpClientFactory, ILogger<MidtransPaymentGateway> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string Name => "Midtrans";
        public string DisplayName => "Midtrans";
        public string Icon => MudBlazor.Icons.Material.Filled.AccountBalanceWallet;
        public bool RequiresRedirect => true;

        public bool IsConfigured(PosSettings settings)
            => settings.MidtransEnabled && !string.IsNullOrWhiteSpace(settings.MidtransServerKey);

        private static string SnapUrl(PosSettings s) => s.MidtransIsProduction
            ? "https://app.midtrans.com"
            : "https://app.sandbox.midtrans.com";

        private static string ApiUrl(PosSettings s) => s.MidtransIsProduction
            ? "https://api.midtrans.com"
            : "https://api.sandbox.midtrans.com";

        public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, PosSettings settings, CancellationToken ct = default)
        {
            if (!IsConfigured(settings))
                return PaymentResult.Fail(Name, "Midtrans belum diaktifkan atau Server Key masih kosong.");

            // Midtrans mensyaratkan gross_amount sama persis dengan jumlah item_details,
            // jadi selisih pembulatan dikirim sebagai satu baris penyesuaian.
            var items = request.Items
                .Select(i => new
                {
                    id = i.Name.Length > 40 ? i.Name[..40] : i.Name,
                    price = Math.Round(i.Price, 0, MidpointRounding.AwayFromZero),
                    quantity = i.Quantity,
                    name = i.Name.Length > 50 ? i.Name[..50] : i.Name
                })
                .ToList();

            var itemsTotal = items.Sum(i => i.price * i.quantity);
            var gross = Math.Round(request.Amount, 0, MidpointRounding.AwayFromZero);
            var adjustment = gross - itemsTotal;

            var itemDetails = new List<object>(items);
            if (adjustment != 0)
            {
                itemDetails.Add(new
                {
                    id = "ADJ",
                    price = adjustment,
                    quantity = 1,
                    name = adjustment > 0 ? "Pajak & biaya" : "Diskon & pembulatan"
                });
            }

            var body = new Dictionary<string, object?>
            {
                ["transaction_details"] = new
                {
                    order_id = request.InvoiceNumber,
                    gross_amount = gross
                },
                ["item_details"] = itemDetails,
                ["customer_details"] = new
                {
                    first_name = string.IsNullOrWhiteSpace(request.CustomerName) ? "Pelanggan" : request.CustomerName,
                    email = request.CustomerEmail,
                    phone = request.CustomerPhone
                },
                ["callbacks"] = new { finish = request.SuccessUrl }
            };

            try
            {
                using var client = CreateClient(settings, SnapUrl(settings));
                using var response = await client.PostAsJsonAsync("/snap/v1/transactions", body, ct);
                var json = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Midtrans menolak order {Invoice}: {Status} {Body}", request.InvoiceNumber, response.StatusCode, json);
                    return PaymentResult.Fail(Name, ReadErrorMessage(json, response.StatusCode.ToString()));
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new PaymentResult
                {
                    Success = true,
                    Provider = Name,
                    State = PaymentState.Pending,
                    // order_id yang dipakai untuk pengecekan status, bukan token Snap.
                    Reference = request.InvoiceNumber,
                    RedirectUrl = root.TryGetProperty("redirect_url", out var url) ? url.GetString() : null,
                    Message = "Sesi pembayaran Midtrans dibuat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menghubungi Midtrans untuk {Invoice}", request.InvoiceNumber);
                return PaymentResult.Fail(Name, $"Tidak dapat menghubungi Midtrans: {ex.Message}");
            }
        }

        public async Task<PaymentResult> CheckStatusAsync(string reference, PosSettings settings, CancellationToken ct = default)
        {
            if (!IsConfigured(settings))
                return PaymentResult.Fail(Name, "Midtrans belum dikonfigurasi.");

            try
            {
                using var client = CreateClient(settings, ApiUrl(settings));
                using var response = await client.GetAsync($"/v2/{Uri.EscapeDataString(reference)}/status", ct);
                var json = await response.Content.ReadAsStringAsync(ct);

                // 404 berarti pelanggan belum menyelesaikan pembayaran apa pun.
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return new PaymentResult { Success = true, Provider = Name, State = PaymentState.Pending, Reference = reference };

                if (!response.IsSuccessStatusCode)
                    return PaymentResult.Fail(Name, ReadErrorMessage(json, response.StatusCode.ToString()));

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var status = root.TryGetProperty("transaction_status", out var ts) ? ts.GetString() : null;
                var fraud = root.TryGetProperty("fraud_status", out var fs) ? fs.GetString() : null;

                return new PaymentResult
                {
                    Success = true,
                    Provider = Name,
                    State = MapState(status, fraud),
                    Reference = reference
                };
            }
            catch (Exception ex)
            {
                return PaymentResult.Fail(Name, $"Tidak dapat menghubungi Midtrans: {ex.Message}");
            }
        }

        private HttpClient CreateClient(PosSettings settings, string baseUrl)
        {
            var client = _httpClientFactory.CreateClient(nameof(MidtransPaymentGateway));
            client.BaseAddress = new Uri(baseUrl);
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.MidtransServerKey.Trim() + ":"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        /// <summary>
        /// capture hanya dianggap lunas bila fraud_status accept; challenge masih menunggu
        /// keputusan manual sehingga tetap Pending.
        /// </summary>
        internal static PaymentState MapState(string? status, string? fraudStatus) => status?.ToLowerInvariant() switch
        {
            "settlement" => PaymentState.Paid,
            "capture" => string.Equals(fraudStatus, "challenge", StringComparison.OrdinalIgnoreCase)
                ? PaymentState.Pending
                : PaymentState.Paid,
            "pending" => PaymentState.Pending,
            "deny" or "cancel" or "failure" => PaymentState.Failed,
            "expire" => PaymentState.Expired,
            _ => PaymentState.Unknown
        };

        private static string ReadErrorMessage(string json, string fallback)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error_messages", out var errors) && errors.ValueKind == JsonValueKind.Array)
                    return string.Join("; ", errors.EnumerateArray().Select(e => e.GetString()));
                if (doc.RootElement.TryGetProperty("status_message", out var m))
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
