using StockAnalyzer.Models;

namespace StockAnalyzer.Services.Recommendation;

/// <summary>
/// Stock recommendation service interface.
/// </summary>
public interface IRecommendationService
{
    /// <summary>Generate recommendation for a specific stock</summary>
    Task<StockRecommendation> GenerateRecommendationAsync(string stockCode);

    /// <summary>Get top 10 recommended stocks</summary>
    Task<List<TopRecommendation>> GetTopRecommendationsAsync(int count = 10);

    /// <summary>Get recommendations by sector</summary>
    Task<List<StockRecommendation>> GetRecommendationsBySectorAsync(string sector);

    /// <summary>Get latest recommendation for a stock</summary>
    Task<StockRecommendation?> GetLatestRecommendationAsync(string stockCode);

    /// <summary>Refresh all top recommendations</summary>
    Task RefreshTopRecommendationsAsync();

    /// <summary>Get score breakdown for a stock</summary>
    Task<(double technical, double fundamental, double sentiment, double overall)> CalculateScoresAsync(string stockCode);
}
