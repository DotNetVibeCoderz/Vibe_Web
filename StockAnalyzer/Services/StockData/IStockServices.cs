using StockAnalyzer.Models;

namespace StockAnalyzer.Services.StockData;

/// <summary>
/// Main stock data service for fetching and managing stock information.
/// </summary>
public interface IStockDataService
{
    // Stock Emiten
    Task<List<StockEmiten>> GetAllStocksAsync();
    Task<List<StockEmiten>> GetStocksBySectorAsync(string sector);
    Task<StockEmiten?> GetStockByCodeAsync(string stockCode);
    Task<StockEmiten?> GetStockByIdAsync(int id);
    Task<List<string>> GetAllSectorsAsync();
    Task<StockEmiten> AddOrUpdateStockAsync(StockEmiten stock);

    // Technical Data
    Task<List<TechnicalIndicator>> GetTechnicalHistoryAsync(int stockId, DateTime? from = null, DateTime? to = null);
    Task<TechnicalIndicator?> GetLatestTechnicalAsync(int stockId);

    // Fundamental Data
    Task<List<FundamentalData>> GetFundamentalHistoryAsync(int stockId);
    Task<FundamentalData?> GetLatestFundamentalAsync(int stockId);

    // Sentiment Data
    Task<List<SentimentData>> GetSentimentDataAsync(int stockId, int limit = 20);
    Task<List<SectorSentiment>> GetSectorSentimentsAsync();
    Task<SectorSentiment?> GetSectorSentimentAsync(string sector);

    // Market Overview
    Task<int> GetTotalStocksAsync();
    Task<decimal> GetAverageMarketSentimentAsync();
    Task<Dictionary<string, int>> GetStocksBySectorCountAsync();
}

/// <summary>
/// Technical analysis service for calculating indicators.
/// </summary>
public interface ITechnicalAnalysisService
{
    // Moving Averages
    decimal CalculateMA(List<decimal> prices, int period);
    List<decimal> CalculateMA5(List<decimal> prices);
    List<decimal> CalculateMA10(List<decimal> prices);
    List<decimal> CalculateMA20(List<decimal> prices);
    List<decimal> CalculateMA50(List<decimal> prices);
    List<decimal> CalculateMA200(List<decimal> prices);

    // RSI
    List<decimal> CalculateRSI(List<decimal> prices, int period = 14);

    // MACD
    (List<decimal> macd, List<decimal> signal, List<decimal> histogram) CalculateMACD(
        List<decimal> prices, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9);

    // Bollinger Bands
    (List<decimal> upper, List<decimal> middle, List<decimal> lower) CalculateBollingerBands(
        List<decimal> prices, int period = 20, double multiplier = 2.0);

    // Stochastic
    (List<decimal> k, List<decimal> d) CalculateStochastic(
        List<decimal> high, List<decimal> low, List<decimal> close, int period = 14, int smoothK = 3, int smoothD = 3);

    // ATR
    List<decimal> CalculateATR(List<decimal> high, List<decimal> low, List<decimal> close, int period = 14);

    // Trend Analysis
    string DetermineTrend(List<decimal> prices);
    string DetectCandlestickPattern(decimal open, decimal high, decimal low, decimal close, decimal prevOpen, decimal prevClose);

    // Score calculation
    double CalculateTechnicalScore(List<TechnicalIndicator> indicators);
}

/// <summary>
/// Fundamental analysis service for evaluating company financial health.
/// </summary>
public interface IFundamentalAnalysisService
{
    // Ratio Calculations
    decimal CalculatePER(decimal price, decimal eps);
    decimal CalculatePBV(decimal price, decimal bookValuePerShare);
    decimal CalculateDER(decimal totalDebt, decimal totalEquity);
    decimal CalculateROE(decimal netIncome, decimal totalEquity);
    decimal CalculateEPS(decimal netIncome, decimal outstandingShares);
    decimal CalculateCurrentRatio(decimal currentAssets, decimal currentLiabilities);

    // Health Assessment
    string AssessPER(decimal? per);
    string AssessPBV(decimal? pbv);
    string AssessDER(decimal? der);
    string AssessROE(decimal? roe);
    string AssessGrowth(decimal? revenueGrowth, decimal? earningsGrowth);

    // Overall Score
    double CalculateFundamentalScore(FundamentalData data);
    string GetFundamentalSummary(FundamentalData data);
}

/// <summary>
/// Sentiment analysis service for news sentiment evaluation.
/// </summary>
public interface ISentimentAnalysisService
{
    // Sentiment Analysis
    (double score, string label, double confidence) AnalyzeSentiment(string text);
    double CalculateAggregateSentiment(List<SentimentData> sentiments);

    // Sector Clustering
    Task<List<SectorSentiment>> ClusterSentimentsBySectorAsync(List<SentimentData> allSentiments);

    // Summary
    string GetSentimentSummary(List<SentimentData> sentiments);
}

/// <summary>
/// News scraping service for gathering financial news.
/// </summary>
public interface INewsScrapingService
{
    Task<List<SentimentData>> ScrapeNewsAsync(string stockCode, string? companyName = null);
    Task<List<SentimentData>> ScrapeSectorNewsAsync(string sector);
    Task<List<SentimentData>> ScrapeMarketNewsAsync();
}
