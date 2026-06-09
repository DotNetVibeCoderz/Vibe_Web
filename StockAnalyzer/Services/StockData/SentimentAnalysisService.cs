using System.Text.RegularExpressions;
using StockAnalyzer.Models;

namespace StockAnalyzer.Services.StockData;

/// <summary>
/// Sentiment analysis service for evaluating news sentiment.
/// Uses keyword-based analysis as default, with optional LLM enhancement.
/// </summary>
public class SentimentAnalysisService : ISentimentAnalysisService
{
    // Indonesian positive and negative keywords for sentiment analysis
    private static readonly HashSet<string> PositiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "naik", "meningkat", "tumbuh", "profit", "laba", "positif", "optimis",
        "rekomendasi beli", "target harga naik", "kinerja baik", "ekspansi",
        "dividen", "buyback", "akuisisi", "kerjasama", "kontrak baru",
        "outperform", "overweight", "technical buy", "breakout",
        "cetak rekor", "melonjak", "prospek cerah", "fundamental kuat",
        "undervalued", "murah", "potensi naik", "momentum positif"
    };

    private static readonly HashSet<string> NegativeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "turun", "menurun", "rugi", "negatif", "pesimis", "hati-hati",
        "rekomendasi jual", "target harga turun", "kinerja buruk", "default",
        "gagal bayar", "hukum", "sanksi", "denda", "investigasi",
        "underperform", "underweight", "technical sell", "breakdown",
        "krisis", "resesi", "inflasi tinggi", "suku bunga naik",
        "overvalued", "mahal", "potensi turun", "momentum negatif",
        "restrukturisasi", "PHK", "pemutusan", "bangkrut", "pailit"
    };

    /// <summary>
    /// Analyze sentiment from text using keyword matching.
    /// Returns (score, label, confidence).
    /// </summary>
    public (double score, string label, double confidence) AnalyzeSentiment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0, "neutral", 0.5);

        int positiveCount = PositiveKeywords.Count(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        int negativeCount = NegativeKeywords.Count(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));

        int totalKeywords = positiveCount + negativeCount;

        if (totalKeywords == 0)
            return (0, "neutral", 0.3);

        double rawScore = (double)(positiveCount - negativeCount) / Math.Max(totalKeywords, 1);
        double score = Math.Clamp(rawScore, -1, 1);
        double confidence = Math.Min(0.9, (double)totalKeywords / 20);

        string label = score switch
        {
            > 0.3 => "positive",
            < -0.3 => "negative",
            _ => "neutral"
        };

        return (score, label, confidence);
    }

    /// <summary>
    /// Calculate aggregate sentiment from a list of sentiment data.
    /// </summary>
    public double CalculateAggregateSentiment(List<SentimentData> sentiments)
    {
        if (sentiments == null || sentiments.Count == 0) return 0;
        return sentiments.Average(s => s.SentimentScore);
    }

    /// <summary>
    /// Cluster sentiment data by sector.
    /// </summary>
    public Task<List<SectorSentiment>> ClusterSentimentsBySectorAsync(List<SentimentData> allSentiments)
    {
        var grouped = allSentiments
            .Where(s => !string.IsNullOrEmpty(s.RelatedSector))
            .GroupBy(s => s.RelatedSector!)
            .Select(g => new SectorSentiment
            {
                Sector = g.Key,
                AverageSentiment = g.Average(s => s.SentimentScore),
                NewsCount = g.Count(),
                PositiveCount = g.Count(s => s.SentimentLabel == "positive"),
                NegativeCount = g.Count(s => s.SentimentLabel == "negative"),
                NeutralCount = g.Count(s => s.SentimentLabel == "neutral"),
                AnalysisDate = DateTime.UtcNow
            })
            .ToList();

        return Task.FromResult(grouped);
    }

    /// <summary>
    /// Generate sentiment summary.
    /// </summary>
    public string GetSentimentSummary(List<SentimentData> sentiments)
    {
        if (sentiments == null || sentiments.Count == 0)
            return "No sentiment data available.";

        var avgScore = sentiments.Average(s => s.SentimentScore);
        var positiveCount = sentiments.Count(s => s.SentimentLabel == "positive");
        var negativeCount = sentiments.Count(s => s.SentimentLabel == "negative");
        var neutralCount = sentiments.Count(s => s.SentimentLabel == "neutral");

        var sentimentEmoji = avgScore switch
        {
            > 0.3 => "🟢 Bullish",
            < -0.3 => "🔴 Bearish",
            _ => "🟡 Neutral"
        };

        return $"{sentimentEmoji} | Score: {avgScore:F2} | " +
               $"Positive: {positiveCount} | Negative: {negativeCount} | Neutral: {neutralCount}";
    }
}
