using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Xendit Invoice API (https://api.xendit.co/v2/invoices). Otentikasi memakai HTTP Basic
    /// dengan secret key sebagai username dan password kosong.
    /// </summary>
    public class XenditPaymentGateway : IPaymentGateway
    {
        private const string BaseUrl = "https://api.xendit.co";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<XenditPaymentGateway> _logger;

        public XenditPaymentGateway(IHttpClientFactory httpClientFactory, ILogger<XenditPaymentGateway> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string Name => "Xendit";
        public string DisplayName => "Xendit";
        public string Icon => MudBlazor.Icons.Material.Filled.QrCode2;
        public bool RequiresRedirect => true;

        public bool IsConfigured(PosSettings settings)
            => settings.XenditEnabled && !string.IsNullOrWhiteSpace(settings.XenditSecretKey);

        public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, PosSettings settings, CancellationToken ct = default)
        {
            if (!IsConfigured(settings))
                return PaymentResult.Fail(Name, "Xendit belum diaktifkan atau Secret Key masih kosong.");

            var body = new Dictionary<string, object?>
            {
                ["external_id"] = request.InvoiceNumber,
                ["amount"] = request.Amount,
                ["currency"] = request.CurrencyCode,
                ["description"] = $"Pembayaran {request.InvoiceNumber} - {settings.StoreName}",
                ["success_redirect_url"] = request.SuccessUrl,
                ["failure_redirect_url"] = request.FailureUrl,
                ["items"] = request.Items.Select(i => new
                {
                    name = i.Name,
                    quantity = i.Quantity,
                    price = i.Price
                }).ToArray()
            };

            if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
                body["payer_email"] = request.CustomerEmail;

            if (!string.IsNullOrWhiteSpace(request.CustomerName) || !string.IsNullOrWhiteSpace(request.CustomerPhone))
            {
                body["customer"] = new
                {
                    given_names = string.IsNullOrWhiteSpace(request.CustomerName) ? "Pelanggan" : request.CustomerName,
                    email = request.CustomerEmail,
                    mobile_number = request.CustomerPhone
                };
            }

            try
            {
                using var client = CreateClient(settings);
                using var response = await client.PostAsJsonAsync("/v2/invoices", body, ct);
                var json = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Xendit menolak invoice {Invoice}: {Status} {Body}", request.InvoiceNumber, response.StatusCode, json);
                    return PaymentResult.Fail(Name, ReadErrorMessage(json, response.StatusCode.ToString()));
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new PaymentResult
                {
                    Success = true,
                    Provider = Name,
                    State = MapState(root.TryGetProperty("status", out var st) ? st.GetString() : null),
                    Reference = root.TryGetProperty("id", out var id) ? id.GetString() : null,
                    RedirectUrl = root.TryGetProperty("invoice_url", out var url) ? url.GetString() : null,
                    Message = "Invoice Xendit dibuat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menghubungi Xendit untuk {Invoice}", request.InvoiceNumber);
                return PaymentResult.Fail(Name, $"Tidak dapat menghubungi Xendit: {ex.Message}");
            }
        }

        public async Task<PaymentResult> CheckStatusAsync(string reference, PosSettings settings, CancellationToken ct = default)
        {
            if (!IsConfigured(settings))
                return PaymentResult.Fail(Name, "Xendit belum dikonfigurasi.");

            try
            {
                using var client = CreateClient(settings);
                using var response = await client.GetAsync($"/v2/invoices/{Uri.EscapeDataString(reference)}", ct);
                var json = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                    return PaymentResult.Fail(Name, ReadErrorMessage(json, response.StatusCode.ToString()));

                using var doc = JsonDocument.Parse(json);
                var state = MapState(doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : null);

                return new PaymentResult
                {
                    Success = true,
                    Provider = Name,
                    State = state,
                    Reference = reference
                };
            }
            catch (Exception ex)
            {
                return PaymentResult.Fail(Name, $"Tidak dapat menghubungi Xendit: {ex.Message}");
            }
        }

        private HttpClient CreateClient(PosSettings settings)
        {
            var client = _httpClientFactory.CreateClient(nameof(XenditPaymentGateway));
            client.BaseAddress = new Uri(BaseUrl);
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.XenditSecretKey.Trim() + ":"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
            return client;
        }

        /// <summary>PENDING / PAID / SETTLED / EXPIRED dipetakan ke status internal.</summary>
        internal static PaymentState MapState(string? status) => status?.ToUpperInvariant() switch
        {
            "PAID" or "SETTLED" or "COMPLETED" => PaymentState.Paid,
            "PENDING" => PaymentState.Pending,
            "EXPIRED" => PaymentState.Expired,
            "FAILED" or "STOPPED" => PaymentState.Failed,
            _ => PaymentState.Unknown
        };

        private static string ReadErrorMessage(string json, string fallback)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var m)) return m.GetString() ?? fallback;
                if (doc.RootElement.TryGetProperty("error_code", out var c)) return c.GetString() ?? fallback;
            }
            catch (JsonException)
            {
                // biarkan memakai fallback
            }
            return fallback;
        }
    }
}
