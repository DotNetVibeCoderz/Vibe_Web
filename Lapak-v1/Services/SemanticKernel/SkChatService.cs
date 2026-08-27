using System.ComponentModel;
using System.Text;
using Lapak.Data;
using Lapak.Models;
using Lapak.Models.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Lapak.Services.SemanticKernel;

public interface ISkChatService
{
    Task<string> ChatAsync(string chatbotType, string userMessage, List<ChatMessageContent>? history = null, string? imageUrl = null, CancellationToken ct = default);
    IAsyncEnumerable<string> ChatStreamAsync(string chatbotType, string userMessage, List<ChatMessageContent>? history = null, string? imageUrl = null, CancellationToken ct = default);
}

public class SkChatService : ISkChatService
{
    private readonly AiConfig _aiConfig;
    private readonly LapakDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SkChatService> _logger;
    private static readonly string[] FallbackOrder = { "OpenAI", "Gemini", "Anthropic", "Ollama" };

    public SkChatService(
        IOptions<AiConfig> aiConfig, LapakDbContext db, IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider, ILogger<SkChatService> logger)
    { _aiConfig = aiConfig.Value; _db = db; _httpClientFactory = httpClientFactory; _serviceProvider = serviceProvider; _logger = logger; }

    private Kernel GetKernel(string providerName)
    {
        var config = _aiConfig.Providers.GetValueOrDefault(providerName)
            ?? _aiConfig.Providers.GetValueOrDefault(_aiConfig.DefaultProvider)
            ?? throw new InvalidOperationException($"No AI provider for '{providerName}'");

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(modelId: config.Model, endpoint: new Uri(config.BaseUrl), apiKey: config.ApiKey);
        builder.Services.AddSingleton(_db);
        builder.Services.AddSingleton(_httpClientFactory);

        builder.Plugins.AddFromType<ProductSearchTools>("ProductSearch");
        builder.Plugins.AddFromType<StoreSearchTools>("StoreSearch");
        builder.Plugins.AddFromType<OrderTools>("OrderTools");
        builder.Plugins.AddFromType<GeneralTools>("GeneralTools");

        return builder.Build();
    }

    public async Task<string> ChatAsync(string chatbotType, string userMessage, List<ChatMessageContent>? history = null, string? imageUrl = null, CancellationToken ct = default)
    {
        foreach (var provider in GetProviderPriorityList())
        {
            try { return await ChatWithProviderAsync(chatbotType, provider, userMessage, history, imageUrl, ct); }
            catch (Exception ex) { if (!_aiConfig.FallbackEnabled) throw; _logger.LogWarning(ex, "SK {Provider} failed", provider); }
        }
        return "❌ Maaf, semua provider AI sedang tidak tersedia. Silakan coba lagi nanti ya! 🙏";
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(string chatbotType, string userMessage, List<ChatMessageContent>? history = null, string? imageUrl = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var provider in GetProviderPriorityList())
        {
            var tokens = await TryStreamWithProviderAsync(chatbotType, provider, userMessage, history, imageUrl, ct);
            if (tokens != null && tokens.Count > 0) { foreach (var t in tokens) yield return t; yield break; }
            if (!_aiConfig.FallbackEnabled) break;
        }
        yield return "❌ Maaf, semua provider AI sedang tidak tersedia. Silakan coba lagi nanti ya! 🙏";
    }

    private async Task<string> ChatWithProviderAsync(string chatbotType, string provider, string userMessage, List<ChatMessageContent>? history, string? imageUrl, CancellationToken ct)
    {
        var kernel = GetKernel(provider);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var botConfig = _aiConfig.ChatBots.GetValueOrDefault(chatbotType);

        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = botConfig?.Temperature ?? 0.7, MaxTokens = botConfig?.MaxTokens ?? 2000,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var chatHistory = new ChatHistory(botConfig?.SystemPrompt ?? "Kamu adalah asisten AI yang membantu.");
        if (history != null) foreach (var m in history.TakeLast(15)) chatHistory.Add(m);
        if (!string.IsNullOrEmpty(imageUrl))
            chatHistory.AddUserMessage(new ChatMessageContentItemCollection { new TextContent(userMessage), new ImageContent(new Uri(imageUrl)) });
        else chatHistory.AddUserMessage(userMessage);

        var result = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel, ct);
        return result.Content ?? "Maaf, tidak ada respon dari AI.";
    }

    private async Task<List<string>?> TryStreamWithProviderAsync(string chatbotType, string provider, string userMessage, List<ChatMessageContent>? history, string? imageUrl, CancellationToken ct)
    {
        try
        {
            var kernel = GetKernel(provider);
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var botConfig = _aiConfig.ChatBots.GetValueOrDefault(chatbotType);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = botConfig?.Temperature ?? 0.7, MaxTokens = botConfig?.MaxTokens ?? 2000,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var chatHistory = new ChatHistory(botConfig?.SystemPrompt ?? "Kamu adalah asisten AI yang membantu.");
            if (history != null) foreach (var m in history.TakeLast(15)) chatHistory.Add(m);
            if (!string.IsNullOrEmpty(imageUrl))
                chatHistory.AddUserMessage(new ChatMessageContentItemCollection { new TextContent(userMessage), new ImageContent(new Uri(imageUrl)) });
            else chatHistory.AddUserMessage(userMessage);

            var tokens = new List<string>();
            await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(chatHistory, settings, kernel, ct))
                if (!string.IsNullOrEmpty(chunk.Content)) tokens.Add(chunk.Content);
            return tokens.Count > 0 ? tokens : null;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Streaming SK {Provider} failed", provider); return null; }
    }

    private List<string> GetProviderPriorityList()
    {
        var providers = new List<string> { _aiConfig.DefaultProvider };
        foreach (var p in FallbackOrder) if (!providers.Contains(p)) providers.Add(p);
        return providers;
    }
}

// ================================================================
// KERNEL FUNCTIONS — all searches are case-insensitive
// ================================================================

public class ProductSearchTools
{
    private readonly LapakDbContext _db;
    public ProductSearchTools(LapakDbContext db) => _db = db;

    [KernelFunction("search_products")]
    [Description("Mencari produk di database berdasarkan nama, kategori, harga, rating. SEMUA pencarian teks TIDAK case-sensitive.")]
    public async Task<string> SearchProducts(
        [Description("Kata kunci (opsional)")] string? keyword = null,
        [Description("Nama kategori (opsional)")] string? category = null,
        [Description("Harga minimum (opsional)")] decimal? minPrice = null,
        [Description("Harga maksimum (opsional)")] decimal? maxPrice = null,
        [Description("Rating minimum 0-5 (opsional)")] double? minRating = null,
        [Description("Urutan: price_asc, price_desc, rating, popular, newest")] string orderBy = "popular",
        [Description("Jumlah maks hasil (default 10)")] int limit = 10)
    {
        try
        {
            var query = _db.Products.Include(p => p.Category).Include(p => p.Store).Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower().Trim();
                query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)) ||
                    (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = category.ToLower().Trim();
                query = query.Where(p => p.Category != null &&
                    (p.Category.Name.ToLower().Contains(cat) ||
                    (p.Category.ParentCategory != null && p.Category.ParentCategory.Name.ToLower().Contains(cat)) ||
                    p.Category.Slug.ToLower().Contains(cat)));
            }

            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);
            if (minRating.HasValue) query = query.Where(p => p.AverageRating >= minRating.Value);

            query = orderBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.AverageRating),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.SoldCount)
            };

            var products = await query.Take(limit).ToListAsync();
            if (products.Count == 0) return "Tidak ada produk yang cocok dengan kriteria.";

            var sb = new StringBuilder(); sb.AppendLine($"📦 Ditemukan {products.Count} produk:");
            foreach (var p in products) sb.AppendLine($"- **{p.Name}** | Rp{p.Price:N0} | ⭐{p.AverageRating:F1} | 🏪{p.Store?.Name} | {p.Category?.Name} | Stok:{p.Stock} | slug:{p.Slug}");
            return sb.ToString();
        }
        catch (Exception ex) { return $"❌ Error: {ex.Message}"; }
    }

    [KernelFunction("get_product_detail")]
    [Description("Detail lengkap produk berdasarkan slug atau nama (case-insensitive)")]
    public async Task<string> GetProductDetail([Description("Slug atau nama produk")] string id)
    {
        var idLower = id.ToLower().Trim();
        var p = await _db.Products.Include(x => x.Category).Include(x => x.Store)
            .FirstOrDefaultAsync(x => x.Slug.ToLower() == idLower || x.Name.ToLower().Contains(idLower));
        if (p == null) return $"Produk '{id}' tidak ditemukan. Coba gunakan slug yang tepat atau cari dengan keyword yang lebih spesifik.";
        return $"📦 **{p.Name}**\n💰 Rp{p.Price:N0}" + (p.OriginalPrice > p.Price ? $" (Diskon Rp{p.OriginalPrice:N0})" : "") +
               $"\n⭐ {p.AverageRating:F1} ({p.RatingCount})\n🏪 {p.Store?.Name}\n📂 {p.Category?.Name}\n📋 Stok:{p.Stock}\n📝 {p.Description ?? ""}";
    }

    [KernelFunction("get_promos")]
    [Description("Daftar promo dan voucher aktif")]
    public async Task<string> GetActivePromos()
    {
        var now = DateTime.UtcNow;
        var promos = await _db.ProductPromos.Include(p => p.Product).Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now).Take(10).ToListAsync();
        var vouchers = await _db.Vouchers.Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now && v.CurrentUsage < v.MaxUsage).Take(10).ToListAsync();
        var sb = new StringBuilder(); sb.AppendLine("🎫 Promo Aktif:");
        foreach (var p in promos) sb.AppendLine($"- {p.Name} ({p.Type} {p.Value}) utk {p.Product?.Name} s/d {p.EndDate:dd/MM}");
        sb.AppendLine("\n🎟️ Voucher:");
        foreach (var v in vouchers) sb.AppendLine($"- **{v.Code}**: {v.Name} ({v.Type} {v.Value})" + (v.MinPurchase.HasValue ? $" min Rp{v.MinPurchase:N0}" : ""));
        if (!promos.Any() && !vouchers.Any()) sb.AppendLine("Tidak ada promo/voucher aktif.");
        return sb.ToString();
    }
}

public class StoreSearchTools
{
    private readonly LapakDbContext _db;
    public StoreSearchTools(LapakDbContext db) => _db = db;

    [KernelFunction("search_stores")]
    [Description("Mencari toko berdasarkan nama, rating, kota (case-insensitive)")]
    public async Task<string> SearchStores(
        [Description("Nama toko (opsional)")] string? keyword = null,
        [Description("Rating minimum (opsional)")] double? minRating = null,
        [Description("Kota (opsional)")] string? city = null,
        [Description("Hanya verified?")] bool verifiedOnly = false,
        [Description("Limit (default 10)")] int limit = 10)
    {
        var q = _db.Stores.Where(s => s.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower().Trim();
            q = q.Where(s => s.Name.ToLower().Contains(kw) ||
                (s.Description != null && s.Description.ToLower().Contains(kw)));
        }

        if (minRating.HasValue) q = q.Where(s => s.AverageRating >= minRating.Value);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.ToLower().Trim();
            q = q.Where(s => s.City != null && s.City.ToLower().Contains(c));
        }

        if (verifiedOnly) q = q.Where(s => s.IsVerified);

        var stores = await q.OrderByDescending(s => s.AverageRating).Take(limit).ToListAsync();
        if (stores.Count == 0) return "Tidak ada toko yang cocok.";
        var sb = new StringBuilder(); sb.AppendLine($"🏪 {stores.Count} toko:");
        foreach (var s in stores) sb.AppendLine($"- **{s.Name}** | ⭐{s.AverageRating:F1} | 📍{s.City} | {(s.IsVerified ? "✅" : "⏳")} | {s.TotalProducts} produk | slug:{s.Slug}");
        return sb.ToString();
    }
}

public class OrderTools
{
    private readonly LapakDbContext _db;
    public OrderTools(LapakDbContext db) => _db = db;

    [KernelFunction("check_order_status")]
    [Description("Cek status pesanan berdasarkan nomor order (case-insensitive)")]
    public async Task<string> CheckOrderStatus([Description("Nomor order, contoh: LPK-250101-0001")] string orderNumber)
    {
        var on = orderNumber.Trim();
        var o = await _db.Orders
            .Include(x => x.OrderItems).ThenInclude(x => x.Product)
            .Include(x => x.ShippingTrackings)
            .FirstOrDefaultAsync(x => x.OrderNumber.ToLower() == on.ToLower());

        if (o == null) return $"❌ Pesanan '{orderNumber}' tidak ditemukan. Pastikan format nomor pesanan benar (contoh: LPK-250101-0001).";

        var sb = new StringBuilder();
        sb.AppendLine($"📋 #{o.OrderNumber} | Status: {o.Status} | Bayar: {o.PaymentStatus} | Total: Rp{o.GrandTotal:N0}");
        sb.AppendLine($"Kurir: {o.ShippingCourier} | Resi: {o.TrackingNumber ?? "-"}");
        sb.AppendLine("Produk:");
        foreach (var i in o.OrderItems) sb.AppendLine($"- {i.Product?.Name} x{i.Quantity} @ Rp{i.Price:N0}");
        if (o.ShippingTrackings.Any()) { sb.AppendLine("📦 Tracking:"); foreach (var t in o.ShippingTrackings.OrderByDescending(t => t.EventDate)) sb.AppendLine($"  [{t.EventDate:dd/MM HH:mm}] {t.Status}: {t.Description} @ {t.Location}"); }
        return sb.ToString();
    }
}

public class GeneralTools
{
    private readonly IHttpClientFactory _httpClientFactory;
    public GeneralTools(IHttpClientFactory f) => _httpClientFactory = f;

    [KernelFunction("get_current_time")]
    [Description("Waktu saat ini UTC dan WIB")]
    public string GetCurrentTime() { var u = DateTime.UtcNow; return $"🕐 UTC: {u:yyyy-MM-dd HH:mm:ss} | WIB: {u.AddHours(7):yyyy-MM-dd HH:mm:ss}"; }

    [KernelFunction("calculate")]
    [Description("Kalkulasi matematika")]
    public string Calculate([Description("Ekspresi, contoh: 2+2*3")] string expr)
    {
        try { return $"🧮 {expr} = **{new System.Data.DataTable().Compute(expr.Replace("^", "**"), null)}**"; }
        catch (Exception ex) { return $"❌ {ex.Message}"; }
    }

    [KernelFunction("search_internet")]
    [Description("Cari informasi (simulasi)")]
    public async Task<string> SearchInternet([Description("Query")] string q)
    {
        await Task.CompletedTask;
        return $"🔍 \"{q}\": Gunakan search_products / search_stores untuk database Lapak.";
    }
}
