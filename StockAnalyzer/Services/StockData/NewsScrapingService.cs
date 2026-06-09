using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StockAnalyzer.Models;

namespace StockAnalyzer.Services.StockData;

/// <summary>
/// News scraping service for gathering financial news from various sources.
/// With fallback to simulated data for demo purposes.
/// </summary>
public class NewsScrapingService : INewsScrapingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ISentimentAnalysisService _sentimentService;
    private readonly ILogger<NewsScrapingService> _logger;
    private readonly Random _random = new();

    public NewsScrapingService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ISentimentAnalysisService sentimentService,
        ILogger<NewsScrapingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _sentimentService = sentimentService;
        _logger = logger;
    }

    /// <summary>
    /// Scrape news related to a specific stock.
    /// Falls back to simulated data if scraping fails.
    /// </summary>
    public async Task<List<SentimentData>> ScrapeNewsAsync(string stockCode, string? companyName = null)
    {
        var news = new List<SentimentData>();
        bool scrapingEnabled = _configuration.GetValue<bool>("NewsScraping:Enabled");

        if (scrapingEnabled)
        {
            try
            {
                // Attempt real scraping - this is a placeholder for actual implementation
                news = await TryScrapeStockNewsAsync(stockCode, companyName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scrape news for {StockCode}, using simulated data", stockCode);
            }
        }

        // If scraping failed or is disabled, generate simulated data
        if (news.Count == 0)
        {
            news = GenerateSimulatedNews(stockCode, companyName ?? stockCode);
        }

        // Analyze sentiment for each news item
        foreach (var item in news)
        {
            var (score, label, confidence) = _sentimentService.AnalyzeSentiment(
                $"{item.Title} {item.Content ?? ""}");
            item.SentimentScore = score;
            item.SentimentLabel = label;
            item.SentimentConfidence = confidence;
            item.IsAnalyzed = true;
            item.CreatedAt = DateTime.UtcNow;
        }

        return news;
    }

    /// <summary>
    /// Scrape news for a specific sector.
    /// </summary>
    public async Task<List<SentimentData>> ScrapeSectorNewsAsync(string sector)
    {
        var news = GenerateSimulatedSectorNews(sector);

        foreach (var item in news)
        {
            var (score, label, confidence) = _sentimentService.AnalyzeSentiment(
                $"{item.Title} {item.Content ?? ""}");
            item.SentimentScore = score;
            item.SentimentLabel = label;
            item.SentimentConfidence = confidence;
            item.IsAnalyzed = true;
        }

        return news;
    }

    /// <summary>
    /// Scrape general market news.
    /// </summary>
    public async Task<List<SentimentData>> ScrapeMarketNewsAsync()
    {
        var news = new List<SentimentData>();

        var sectors = new[] { "Banking", "Technology", "Mining", "Consumer", "Infrastructure", "Energy", "Healthcare" };
        foreach (var sector in sectors)
        {
            news.AddRange(await ScrapeSectorNewsAsync(sector));
        }

        return news;
    }

    // ==================== Private Helpers ====================

    private async Task<List<SentimentData>> TryScrapeStockNewsAsync(string stockCode, string? companyName)
    {
        // Real scraping implementation would go here
        // This would use HtmlAgilityPack or similar to scrape financial news sites
        // For now, return empty to trigger simulated data
        return await Task.FromResult(new List<SentimentData>());
    }

    /// <summary>
    /// Generate simulated news for demonstration purposes.
    /// </summary>
    private List<SentimentData> GenerateSimulatedNews(string stockCode, string companyName)
    {
        var newsTemplates = new List<(string title, string sentiment)>
        {
            ($"{companyName} ({stockCode}) Cetak Laba Bersih Rp 2.5 Triliun di Q1 2025", "positive"),
            ($"Analis Rekomendasikan Beli {stockCode}, Target Harga Naik 15%", "positive"),
            ($"{companyName} Ekspansi Bisnis ke Pasar Regional", "positive"),
            ($"Investor Asing Borong Saham {stockCode} Senilai Rp 500 Miliar", "positive"),
            ($"{stockCode} Bagikan Dividen Rp 150 per Saham", "positive"),
            ($"{companyName} Raih Kontrak Baru Senilai Rp 1 Triliun", "positive"),
            ($"Kinerja {stockCode} Melampaui Ekspektasi Analis", "positive"),
            ($"{companyName} Rilis Produk Baru, Prospek Cerah", "positive"),

            ($"Hati-hati, {stockCode} Mengalami Penurunan Pendapatan", "negative"),
            ($"{companyName} Terkena Dampak Regulasi Baru", "negative"),
            ($"Volume Penjualan {stockCode} Anjlok 20% YoY", "negative"),
            ($"Analis Pangkas Target Harga {stockCode}", "negative"),
            ($"Sentimen Negatif Bebani Pergerakan {stockCode}", "negative"),
            ($"{companyName} Hadapi Tantangan Makro Ekonomi", "negative"),

            ($"{stockCode} Masuk Radar Investor Institusi", "neutral"),
            ($"Update Portofolio: Posisi {stockCode} dalam Indeks", "neutral"),
            ($"{companyName} Gelar RUPS Tahunan Bulan Depan", "neutral"),
            ($"Analis Soroti Valuasi {stockCode} yang Menarik", "neutral"),
        };

        var articles = new List<SentimentData>();
        int count = _random.Next(8, 15);

        var selected = newsTemplates.OrderBy(_ => _random.Next()).Take(count).ToList();

        foreach (var (title, _) in selected)
        {
            articles.Add(new SentimentData
            {
                Title = title,
                Content = $"Berita terkait {companyName} ({stockCode}). " +
                          "Analis pasar memberikan pandangan terhadap pergerakan saham ini.",
                Publisher = GetRandomPublisher(),
                SourceUrl = $"https://example.com/news/{Guid.NewGuid():N}",
                PublishedDate = DateTime.UtcNow.AddDays(-_random.Next(0, 14)),
                RelatedSector = "Market",
                CreatedAt = DateTime.UtcNow
            });
        }

        return articles;
    }

    private List<SentimentData> GenerateSimulatedSectorNews(string sector)
    {
        var newsTemplates = new List<(string title, string sentiment)>
        {
            ($"Sektor {sector} Catat Pertumbuhan Tertinggi di Q1 2025", "positive"),
            ($"Prospek Sektor {sector} Cerah di Tengah Pemulihan Ekonomi", "positive"),
            ($"Investor Mulai Koleksi Saham Sektor {sector}", "positive"),
            ($"Analis Revisi Naik Proyeksi Sektor {sector}", "positive"),

            ($"Sentimen Hati-hati Melanda Sektor {sector}", "negative"),
            ($"Tekanan Inflasi Bayangi Sektor {sector}", "negative"),

            ($"Pemerintah Terbitkan Kebijakan Baru untuk Sektor {sector}", "neutral"),
            ($"Sektor {sector} dalam Fokus Raker DPR", "neutral"),
        };

        var articles = new List<SentimentData>();
        int count = _random.Next(3, 6);

        foreach (var (title, _) in newsTemplates.OrderBy(_ => _random.Next()).Take(count))
        {
            articles.Add(new SentimentData
            {
                Title = title,
                Content = $"Perkembangan terbaru dari sektor {sector} di pasar modal Indonesia.",
                Publisher = GetRandomPublisher(),
                SourceUrl = $"https://example.com/sector/{Guid.NewGuid():N}",
                PublishedDate = DateTime.UtcNow.AddDays(-_random.Next(0, 7)),
                RelatedSector = sector,
                CreatedAt = DateTime.UtcNow
            });
        }

        return articles;
    }

    private string GetRandomPublisher()
    {
        var publishers = new[] { "CNBC Indonesia", "Bisnis.com", "Kontan", "Investor Daily",
            "Bloomberg Technoz", "IDX Channel", "Market Bisnis", "Detik Finance" };
        return publishers[_random.Next(publishers.Length)];
    }
}
