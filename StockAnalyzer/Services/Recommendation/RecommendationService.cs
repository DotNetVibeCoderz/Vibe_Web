using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Data;
using StockAnalyzer.Models;
using StockAnalyzer.Services.LLM;
using StockAnalyzer.Services.StockData;

namespace StockAnalyzer.Services.Recommendation;

/// <summary>
/// Recommendation engine that combines technical, fundamental, and sentiment analysis
/// with optional LLM review to generate stock recommendations.
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly AppDbContext _db;
    private readonly IStockDataService _stockData;
    private readonly ITechnicalAnalysisService _technicalAnalysis;
    private readonly IFundamentalAnalysisService _fundamentalAnalysis;
    private readonly ISentimentAnalysisService _sentimentAnalysis;
    private readonly ILLMService _llmService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        AppDbContext db,
        IStockDataService stockData,
        ITechnicalAnalysisService technicalAnalysis,
        IFundamentalAnalysisService fundamentalAnalysis,
        ISentimentAnalysisService sentimentAnalysis,
        ILLMService llmService,
        IConfiguration configuration,
        ILogger<RecommendationService> logger)
    {
        _db = db;
        _stockData = stockData;
        _technicalAnalysis = technicalAnalysis;
        _fundamentalAnalysis = fundamentalAnalysis;
        _sentimentAnalysis = sentimentAnalysis;
        _llmService = llmService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generate a comprehensive recommendation for a specific stock.
    /// </summary>
    public async Task<StockRecommendation> GenerateRecommendationAsync(string stockCode)
    {
        var stock = await _stockData.GetStockByCodeAsync(stockCode);
        if (stock == null)
            throw new ArgumentException($"Stock '{stockCode}' not found");

        // Get weights from config
        var techWeight = _configuration.GetValue<double>("LLM:TechnicalWeight", 0.35);
        var fundWeight = _configuration.GetValue<double>("LLM:FundamentalWeight", 0.35);
        var sentWeight = _configuration.GetValue<double>("LLM:SentimentWeight", 0.30);

        // 1. Technical Analysis
        var technicalData = await _stockData.GetTechnicalHistoryAsync(stock.Id);
        var techScore = _technicalAnalysis.CalculateTechnicalScore(technicalData);
        var latestTech = await _stockData.GetLatestTechnicalAsync(stock.Id);
        var techSummary = GenerateTechnicalSummary(latestTech, technicalData);

        // 2. Fundamental Analysis
        var fundamentalData = await _stockData.GetLatestFundamentalAsync(stock.Id);
        var fundScore = fundamentalData != null
            ? _fundamentalAnalysis.CalculateFundamentalScore(fundamentalData)
            : 50;
        var fundSummary = fundamentalData != null
            ? _fundamentalAnalysis.GetFundamentalSummary(fundamentalData)
            : "Fundamental data not available";

        // 3. Sentiment Analysis
        var sentiments = await _stockData.GetSentimentDataAsync(stock.Id);
        var sentScore = sentiments.Count > 0
            ? _sentimentAnalysis.CalculateAggregateSentiment(sentiments) * 50 + 50  // Convert -1..1 to 0..100
            : 50;
        var sentSummary = _sentimentAnalysis.GetSentimentSummary(sentiments);

        // 4. Calculate overall score (weighted)
        var overallScore = (techScore * techWeight) +
                          (fundScore * fundWeight) +
                          (sentScore * sentWeight);

        // 5. Determine recommendation
        var recommendation = GetRecommendationLabel(overallScore);
        var riskLevel = GetRiskLevel(overallScore, techScore, fundScore);

        // 6. Target price and stop loss
        var currentPrice = stock.CurrentPrice;
        var targetPrice = recommendation switch
        {
            "StrongBuy" => currentPrice * 1.30m,
            "Buy" => currentPrice * 1.15m,
            "Hold" => currentPrice * 1.05m,
            "Sell" => currentPrice * 0.90m,
            "StrongSell" => currentPrice * 0.80m,
            _ => currentPrice
        };
        var stopLoss = currentPrice * 0.93m;

        // 7. LLM Review (optional)
        string? llmReview = null;
        string? llmProvider = null;
        string? llmModel = null;

        var enableLLM = _configuration.GetValue<bool>("LLM:EnableLLMAnalysis");
        if (enableLLM)
        {
            try
            {
                var llmResult = await _llmService.GetStockRecommendationAsync(
                    stockCode,
                    $"Technical Score: {techScore:F1}/100\n{techSummary}",
                    $"Fundamental Score: {fundScore:F1}/100\n{fundSummary}",
                    $"Sentiment Score: {sentScore:F1}/100\n{sentSummary}"
                );

                if (llmResult.IsSuccess)
                {
                    llmReview = llmResult.Content;
                    llmProvider = llmResult.ProviderName;
                    llmModel = llmResult.ModelName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM review failed for {StockCode}", stockCode);
                llmReview = "LLM review unavailable. Using quantitative analysis only.";
            }
        }

        // 8. Create and save recommendation
        var recommendationObj = new StockRecommendation
        {
            StockEmitenId = stock.Id,
            RecommendationDate = DateTime.UtcNow,
            TechnicalScore = techScore,
            FundamentalScore = fundScore,
            SentimentScore = sentScore,
            OverallScore = overallScore,
            Recommendation = recommendation,
            TargetPrice = targetPrice,
            StopLossPrice = stopLoss,
            RiskLevel = riskLevel,
            LLMReview = llmReview,
            LLMProvider = llmProvider,
            LLMModel = llmModel,
            TechnicalSummary = techSummary,
            FundamentalSummary = fundSummary,
            SentimentSummary = sentSummary,
            CreatedAt = DateTime.UtcNow
        };

        _db.StockRecommendations.Add(recommendationObj);
        await _db.SaveChangesAsync();

        return recommendationObj;
    }

    /// <summary>
    /// Get top 10 recommended stocks.
    /// </summary>
    public async Task<List<TopRecommendation>> GetTopRecommendationsAsync(int count = 10)
    {
        // Try to get cached recommendations first
        var cached = await _db.TopRecommendations
            .OrderBy(t => t.Rank)
            .Take(count)
            .ToListAsync();

        if (cached.Count >= count)
        {
            var age = DateTime.UtcNow - cached[0].GeneratedDate;
            if (age.TotalHours < 24)
                return cached;
        }

        // Refresh recommendations
        await RefreshTopRecommendationsAsync();

        return await _db.TopRecommendations
            .OrderBy(t => t.Rank)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Get recommendations filtered by sector.
    /// </summary>
    public async Task<List<StockRecommendation>> GetRecommendationsBySectorAsync(string sector)
    {
        var stocks = await _stockData.GetStocksBySectorAsync(sector);

        var recommendations = new List<StockRecommendation>();
        foreach (var stock in stocks)
        {
            var latest = await _db.StockRecommendations
                .Where(r => r.StockEmitenId == stock.Id)
                .OrderByDescending(r => r.RecommendationDate)
                .FirstOrDefaultAsync();

            if (latest != null)
                recommendations.Add(latest);
        }

        return recommendations
            .OrderByDescending(r => r.OverallScore)
            .ToList();
    }

    /// <summary>
    /// Get latest recommendation for a stock.
    /// </summary>
    public async Task<StockRecommendation?> GetLatestRecommendationAsync(string stockCode)
    {
        var stock = await _stockData.GetStockByCodeAsync(stockCode);
        if (stock == null) return null;

        return await _db.StockRecommendations
            .Where(r => r.StockEmitenId == stock.Id)
            .OrderByDescending(r => r.RecommendationDate)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Refresh the top recommendations cache.
    /// </summary>
    public async Task RefreshTopRecommendationsAsync()
    {
        var stocks = await _stockData.GetAllStocksAsync();
        var scoredStocks = new List<(StockEmiten stock, double score)>();

        foreach (var stock in stocks.Take(50)) // Limit to 50 for performance
        {
            var (tech, fund, sent, overall) = await CalculateScoresAsync(stock.StockCode);
            scoredStocks.Add((stock, overall));
        }

        var topStocks = scoredStocks
            .OrderByDescending(s => s.score)
            .Take(10)
            .ToList();

        // Clear old recommendations
        var oldRecs = await _db.TopRecommendations.ToListAsync();
        _db.TopRecommendations.RemoveRange(oldRecs);

        // Add new recommendations
        int rank = 1;
        foreach (var (stock, score) in topStocks)
        {
            _db.TopRecommendations.Add(new TopRecommendation
            {
                Rank = rank++,
                StockEmitenId = stock.Id,
                StockCode = stock.StockCode,
                CompanyName = stock.CompanyName,
                OverallScore = score,
                Recommendation = GetRecommendationLabel(score),
                Sector = stock.Sector,
                GeneratedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Top recommendations refreshed");
    }

    /// <summary>
    /// Calculate individual scores for a stock.
    /// </summary>
    public async Task<(double technical, double fundamental, double sentiment, double overall)> CalculateScoresAsync(string stockCode)
    {
        var stock = await _stockData.GetStockByCodeAsync(stockCode);
        if (stock == null) return (0, 0, 0, 0);

        var techWeight = _configuration.GetValue<double>("LLM:TechnicalWeight", 0.35);
        var fundWeight = _configuration.GetValue<double>("LLM:FundamentalWeight", 0.35);
        var sentWeight = _configuration.GetValue<double>("LLM:SentimentWeight", 0.30);

        // Technical
        var technicalData = await _stockData.GetTechnicalHistoryAsync(stock.Id);
        var techScore = _technicalAnalysis.CalculateTechnicalScore(technicalData);

        // Fundamental
        var fundamentalData = await _stockData.GetLatestFundamentalAsync(stock.Id);
        var fundScore = fundamentalData != null
            ? _fundamentalAnalysis.CalculateFundamentalScore(fundamentalData) : 50;

        // Sentiment
        var sentiments = await _stockData.GetSentimentDataAsync(stock.Id);
        var sentScore = sentiments.Count > 0
            ? _sentimentAnalysis.CalculateAggregateSentiment(sentiments) * 50 + 50 : 50;

        // Overall
        var overall = (techScore * techWeight) + (fundScore * fundWeight) + (sentScore * sentWeight);

        return (techScore, fundScore, sentScore, overall);
    }

    // ==================== Helpers ====================

    private string GetRecommendationLabel(double score)
    {
        return score switch
        {
            >= 80 => "StrongBuy",
            >= 65 => "Buy",
            >= 45 => "Hold",
            >= 30 => "Sell",
            _ => "StrongSell"
        };
    }

    private string GetRiskLevel(double overallScore, double techScore, double fundScore)
    {
        var volatility = Math.Abs(techScore - fundScore);
        return volatility switch
        {
            > 40 => "High",
            > 20 => "Medium",
            _ => "Low"
        };
    }

    private string GenerateTechnicalSummary(TechnicalIndicator? latest, List<TechnicalIndicator> history)
    {
        if (latest == null) return "No technical data";

        var prices = history.Select(h => h.ClosePrice).ToList();
        var trend = _technicalAnalysis.DetermineTrend(prices);

        var summary = $"Trend: {trend} | ";

        if (latest.RSI.HasValue)
            summary += $"RSI: {latest.RSI:F1} | ";
        if (latest.MACD.HasValue && latest.MACDSignal.HasValue)
            summary += $"MACD: {(latest.MACD > latest.MACDSignal ? "Bullish" : "Bearish")} | ";
        if (latest.MA20.HasValue)
            summary += $"MA20: {latest.MA20:F0} | ";
        if (latest.ClosePrice > 0)
            summary += $"Close: {latest.ClosePrice:F0}";

        return summary;
    }
}
