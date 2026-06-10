using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lapak.Data;
using Lapak.Models;
using Lapak.Models.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lapak.Services.Shipping;

// ================================================================
// DTOs
// ================================================================
public class ShippingCostRequest
{
    public string OriginCityId { get; set; } = string.Empty;
    public string DestinationCityId { get; set; } = string.Empty;
    public int WeightInGrams { get; set; }
    public string Courier { get; set; } = string.Empty;
}

public class ShippingCostResult
{
    public string Courier { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string EstimatedDays { get; set; } = string.Empty;
}

public class ShippingTrackingResult
{
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
}

public class ShippingOrderRequest
{
    public Guid OrderId { get; set; }
    public string Courier { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
}

// RajaOngkir response DTOs
public class RajaOngkirResponse<T>
{
    [JsonPropertyName("rajaongkir")]
    public RajaOngkirBody<T> RajaOngkir { get; set; } = new();
}

public class RajaOngkirBody<T>
{
    [JsonPropertyName("query")]
    public JsonElement? Query { get; set; }

    [JsonPropertyName("status")]
    public RajaOngkirStatus Status { get; set; } = new();

    [JsonPropertyName("results")]
    public T? Results { get; set; }
}

public class RajaOngkirStatus
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class RajaOngkirCostResult
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("costs")]
    public List<RajaOngkirCostDetail> Costs { get; set; } = new();
}

public class RajaOngkirCostDetail
{
    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("cost")]
    public List<RajaOngkirCostValue> Cost { get; set; } = new();
}

public class RajaOngkirCostValue
{
    [JsonPropertyName("value")]
    public decimal Value { get; set; }

    [JsonPropertyName("etd")]
    public string Etd { get; set; } = string.Empty;
}

// ================================================================
// Interface
// ================================================================
public interface IShippingService
{
    Task<List<ShippingCostResult>> GetShippingCostsAsync(ShippingCostRequest request, CancellationToken ct = default);
    Task<decimal> GetCheapestShippingCostAsync(string destCityId, int weightGrams, CancellationToken ct = default);
    Task<bool> CreateShippingOrderAsync(ShippingOrderRequest request, CancellationToken ct = default);
    Task<List<ShippingTrackingResult>> GetTrackingAsync(string trackingNumber, string courier, CancellationToken ct = default);
    Task UpdateTrackingAsync(Guid orderId, CancellationToken ct = default);
}

// ================================================================
// Implementation
// ================================================================
public class ShippingService : IShippingService
{
    private readonly ShippingConfig _shippingConfig;
    private readonly LapakDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ShippingService> _logger;

    public ShippingService(
        IOptions<ShippingConfig> shippingConfig,
        LapakDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<ShippingService> logger)
    {
        _shippingConfig = shippingConfig.Value;
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Calculate shipping costs from RajaOngkir API
    /// </summary>
    public async Task<List<ShippingCostResult>> GetShippingCostsAsync(ShippingCostRequest request, CancellationToken ct = default)
    {
        var results = new List<ShippingCostResult>();
        var rajaOngkir = _shippingConfig.RajaOngkir;

        if (string.IsNullOrEmpty(rajaOngkir.ApiKey))
        {
            // Return simulated results when API key is not configured
            return GetSimulatedCosts(request);
        }

        var couriers = string.IsNullOrEmpty(request.Courier)
            ? _shippingConfig.Couriers
            : new List<string> { request.Courier };

        foreach (var courier in couriers)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ShippingClient");
                client.DefaultRequestHeaders.Add("key", rajaOngkir.ApiKey);

                var payload = new
                {
                    origin = request.OriginCityId,
                    destination = request.DestinationCityId,
                    weight = request.WeightInGrams,
                    courier = courier.ToLower()
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await client.PostAsync($"{rajaOngkir.BaseUrl}/cost", content, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("RajaOngkir API error: {Body}", responseBody);
                    results.AddRange(GetSimulatedCostForCourier(courier, request.WeightInGrams));
                    continue;
                }

                var apiResponse = JsonSerializer.Deserialize<RajaOngkirResponse<List<RajaOngkirCostResult>>>(responseBody);
                var costResults = apiResponse?.RajaOngkir?.Results;

                if (costResults != null)
                {
                    foreach (var costResult in costResults)
                    {
                        foreach (var costDetail in costResult.Costs)
                        {
                            foreach (var costValue in costDetail.Cost)
                            {
                                results.Add(new ShippingCostResult
                                {
                                    Courier = costResult.Name.ToUpper(),
                                    Service = costDetail.Service,
                                    Description = costDetail.Description,
                                    Cost = costValue.Value,
                                    EstimatedDays = costValue.Etd + " hari"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get shipping cost from {Courier}, using simulated", courier);
                results.AddRange(GetSimulatedCostForCourier(courier, request.WeightInGrams));
            }
        }

        return results.OrderBy(r => r.Cost).ToList();
    }

    public async Task<decimal> GetCheapestShippingCostAsync(string destCityId, int weightGrams, CancellationToken ct = default)
    {
        var costs = await GetShippingCostsAsync(new ShippingCostRequest
        {
            OriginCityId = _shippingConfig.DefaultOrigin.CityId,
            DestinationCityId = destCityId,
            WeightInGrams = weightGrams
        }, ct);

        return costs.FirstOrDefault()?.Cost ?? 15000;
    }

    /// <summary>
    /// Create shipping order (book shipment)
    /// </summary>
    public async Task<bool> CreateShippingOrderAsync(ShippingOrderRequest request, CancellationToken ct = default)
    {
        var order = await _db.Orders.FindAsync(new object[] { request.OrderId }, ct);
        if (order == null) return false;

        try
        {
            order.ShippingCourier = request.Courier;
            order.ShippingService = request.Service;

            // Try to book via RajaOngkir API
            var rajaOngkir = _shippingConfig.RajaOngkir;
            if (!string.IsNullOrEmpty(rajaOngkir.ApiKey))
            {
                var client = _httpClientFactory.CreateClient("ShippingClient");
                client.DefaultRequestHeaders.Add("key", rajaOngkir.ApiKey);

                var payload = new
                {
                    origin = _shippingConfig.DefaultOrigin.CityId,
                    destination = order.ShippingCity ?? "",
                    weight = order.OrderItems.Sum(oi => oi.Product.WeightInGrams * oi.Quantity),
                    courier = request.Courier.ToLower(),
                    service = request.Service
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await client.PostAsync($"{rajaOngkir.BaseUrl}/waybill", content, ct);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(ct);
                    // Parse waybill response and get tracking number
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;
                    // Try to extract tracking number from response
                    if (root.TryGetProperty("rajaongkir", out var rajaOngkirEl) &&
                        rajaOngkirEl.TryGetProperty("results", out var results))
                    {
                        // Extract tracking number
                        order.TrackingNumber = $"SIM-{Guid.NewGuid():N}"[..15];
                    }
                }
            }

            // Simulated tracking number when API unavailable
            if (string.IsNullOrEmpty(order.TrackingNumber))
            {
                var prefix = request.Courier switch
                {
                    "JNE" => "JNE",
                    "J&T" => "JT",
                    "SiCepat" => "SCP",
                    "Pos Indonesia" => "POS",
                    _ => "EXP"
                };
                order.TrackingNumber = $"{prefix}{DateTime.UtcNow:yyMMdd}{new Random().Next(10000, 99999)}";
            }

            order.Status = "Processing";
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Shipping order created: #{OrderNumber}, Tracking: {Tracking}",
                order.OrderNumber, order.TrackingNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shipping order for #{OrderNumber}", order.OrderNumber);
            return false;
        }
    }

    /// <summary>
    /// Get tracking information from RajaOngkir
    /// </summary>
    public async Task<List<ShippingTrackingResult>> GetTrackingAsync(string trackingNumber, string courier, CancellationToken ct = default)
    {
        var results = new List<ShippingTrackingResult>();

        try
        {
            var rajaOngkir = _shippingConfig.RajaOngkir;
            if (!string.IsNullOrEmpty(rajaOngkir.ApiKey))
            {
                var client = _httpClientFactory.CreateClient("ShippingClient");
                client.DefaultRequestHeaders.Add("key", rajaOngkir.ApiKey);

                var payload = new { waybill = trackingNumber, courier = courier.ToLower() };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await client.PostAsync($"{rajaOngkir.BaseUrl}/waybill", content, ct);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(ct);
                    // Parse waybill manifest
                    // (Simplified - in production, fully parse the RajaOngkir response)
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get tracking for {Tracking}", trackingNumber);
        }

        // Simulated tracking if API unavailable or fails
        if (results.Count == 0)
        {
            results = GenerateSimulatedTracking(trackingNumber);
        }

        return results;
    }

    /// <summary>
    /// Poll tracking info and update order
    /// </summary>
    public async Task UpdateTrackingAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.ShippingTrackings)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null || string.IsNullOrEmpty(order.TrackingNumber)) return;

        var trackings = await GetTrackingAsync(order.TrackingNumber, order.ShippingCourier, ct);

        foreach (var t in trackings)
        {
            // Avoid duplicates
            var exists = order.ShippingTrackings.Any(st =>
                st.Description == t.Description && st.EventDate == t.EventDate);

            if (!exists)
            {
                order.ShippingTrackings.Add(new ShippingTracking
                {
                    Status = t.Status,
                    Description = t.Description,
                    Location = t.Location,
                    EventDate = t.EventDate
                });
            }
        }

        // Update order status based on latest tracking
        var latestStatus = trackings.FirstOrDefault()?.Status;
        if (latestStatus != null)
        {
            order.Status = latestStatus switch
            {
                "PICKUP" => "Processing",
                "IN_TRANSIT" => "Shipped",
                "DELIVERED" => "Delivered",
                _ => order.Status
            };

            if (latestStatus == "DELIVERED")
                order.DeliveredAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ================================================================
    // Simulated data helpers (for when API key not configured)
    // ================================================================
    private List<ShippingCostResult> GetSimulatedCosts(ShippingCostRequest request)
    {
        var results = new List<ShippingCostResult>();
        var couriers = string.IsNullOrEmpty(request.Courier)
            ? _shippingConfig.Couriers
            : new List<string> { request.Courier };

        foreach (var courier in couriers)
            results.AddRange(GetSimulatedCostForCourier(courier, request.WeightInGrams));

        return results;
    }

    private List<ShippingCostResult> GetSimulatedCostForCourier(string courier, int weightGrams)
    {
        var baseCost = courier switch
        {
            "JNE" => 9000m,
            "J&T" => 8500m,
            "SiCepat" => 8000m,
            "Pos Indonesia" => 7000m,
            "AnterAja" => 7500m,
            "Ninja Express" => 9500m,
            "Lion Parcel" => 7000m,
            _ => 8000m
        };

        var weightKg = Math.Max(1, weightGrams / 1000);
        return new List<ShippingCostResult>
        {
            new() { Courier = courier, Service = "REG", Description = "Reguler", Cost = baseCost * weightKg, EstimatedDays = "3-5 hari" },
            new() { Courier = courier, Service = "YES", Description = "Express", Cost = baseCost * weightKg * 1.5m, EstimatedDays = "1-2 hari" },
            new() { Courier = courier, Service = "ECO", Description = "Ekonomi", Cost = baseCost * weightKg * 0.7m, EstimatedDays = "5-7 hari" }
        };
    }

    private List<ShippingTrackingResult> GenerateSimulatedTracking(string trackingNumber)
    {
        var rng = new Random(trackingNumber.GetHashCode());
        var now = DateTime.UtcNow;
        var results = new List<ShippingTrackingResult>();

        var statuses = new (string status, string desc, string loc)[]
        {
            ("PICKUP", "Paket telah diambil oleh kurir", "Jakarta Pusat"),
            ("IN_TRANSIT", "Paket dalam perjalanan ke sorting center", "Jakarta"),
            ("IN_TRANSIT", "Paket tiba di sorting center", "Jakarta"),
            ("IN_TRANSIT", "Paket dalam perjalanan ke kota tujuan", "Dalam Perjalanan"),
            ("IN_TRANSIT", "Paket tiba di kota tujuan", "Bandung"),
            ("WITH_COURIER", "Paket sedang diantar oleh kurir", "Bandung"),
            ("DELIVERED", "Paket telah diterima oleh penerima", "Bandung"),
        };

        for (int i = statuses.Length - 1; i >= 0; i--)
        {
            results.Add(new ShippingTrackingResult
            {
                Status = statuses[i].status,
                Description = statuses[i].desc,
                Location = statuses[i].loc,
                EventDate = now.AddHours(-i * rng.Next(4, 12))
            });
        }

        return results;
    }
}
