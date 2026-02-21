using MediaMonitoring.Data;
using MediaMonitoring.Models;
using Microsoft.EntityFrameworkCore;

namespace MediaMonitoring.Services
{
    /// <summary>
    /// Service untuk prediksi tren menggunakan analisis statistik dan pattern recognition sederhana
    /// </summary>
    public class AiTrendPredictionService
    {
        private readonly MediaMonitoringContext _context;
        private readonly OsintEngineService _osintService;

        public AiTrendPredictionService(MediaMonitoringContext context, OsintEngineService osintService)
        {
            _context = context;
            _osintService = osintService;
        }

        /// <summary>
        /// Prediksikan tren untuk 24 jam ke depan
        /// </summary>
        public async Task<TrendPrediction> PredictTrendsAsync(int hoursAhead = 24)
        {
            var now = DateTime.UtcNow;
            var historicalData = await _context.MediaPosts
                .Where(p => p.CreatedAt >= now.AddDays(-7))
                .ToListAsync();

            if (historicalData.Count < 10)
            {
                return new TrendPrediction
                {
                    Message = "Insufficient data for prediction",
                    Confidence = 0,
                    PredictedTrends = new List<PredictedTrend>()
                };
            }

            var categoryTrends = await AnalyzeCategoryTrends(historicalData);
            var sentimentForecast = ForecastSentiment(historicalData);
            var emergingTopics = DetectEmergingTopics(historicalData);
            var confidence = CalculateConfidence(historicalData);

            return new TrendPrediction
            {
                GeneratedAt = now,
                ForecastHours = hoursAhead,
                PredictedTrends = categoryTrends,
                SentimentForecast = sentimentForecast,
                EmergingTopics = emergingTopics,
                Confidence = confidence,
                Recommendations = GenerateRecommendations(categoryTrends, sentimentForecast)
            };
        }

        private async Task<List<PredictedTrend>> AnalyzeCategoryTrends(List<MediaPost> posts)
        {
            var predictions = new List<PredictedTrend>();
            var categories = posts.Select(p => p.Category).Distinct();

            foreach (var category in categories)
            {
                var categoryPosts = posts.Where(p => p.Category == category).ToList();
                if (categoryPosts.Count < 5) continue;

                var dailyCounts = categoryPosts
                    .GroupBy(p => p.CreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToList();

                if (dailyCounts.Count < 3) continue;

                var n = dailyCounts.Count;
                var sumX = Enumerable.Range(0, n).Sum();
                var sumY = dailyCounts.Sum(d => d.Count);
                var sumXY = dailyCounts.Select((d, i) => i * d.Count).Sum();
                var sumX2 = Enumerable.Range(0, n).Select(i => i * i).Sum();

                var slope = (n * sumXY - sumX * sumY) / (double)(n * sumX2 - sumX * sumX);
                var intercept = (sumY - slope * sumX) / (double)n;

                var predictedValue = slope * n + intercept;
                var trend = slope > 0.5 ? "RISING" : slope < -0.5 ? "FALLING" : "STABLE";

                var currentAvg = dailyCounts.Average(d => d.Count);
                var changePercent = currentAvg > 0 ? ((predictedValue - currentAvg) / currentAvg * 100) : 0;

                predictions.Add(new PredictedTrend
                {
                    Category = category,
                    CurrentVolume = (int)currentAvg,
                    PredictedVolume = Math.Max(0, (int)predictedValue),
                    Trend = trend,
                    ChangePercent = Math.Round(changePercent, 1),
                    Confidence = Math.Min(95, 50 + Math.Abs(slope) * 10)
                });
            }

            return predictions.OrderByDescending(p => p.ChangePercent).Take(5).ToList();
        }

        private SentimentForecast ForecastSentiment(List<MediaPost> posts)
        {
            var recentSentiment = posts
                .OrderByDescending(p => p.CreatedAt)
                .Take(posts.Count / 2)
                .GroupBy(p => p.Sentiment)
                .ToDictionary(g => g.Key, g => g.Count());

            var olderSentiment = posts
                .OrderBy(p => p.CreatedAt)
                .Take(posts.Count / 2)
                .GroupBy(p => p.Sentiment)
                .ToDictionary(g => g.Key, g => g.Count());

            var positiveShift = GetSentimentChange(recentSentiment, olderSentiment, "Positive");
            var negativeShift = GetSentimentChange(recentSentiment, olderSentiment, "Negative");

            string outlook = "NEUTRAL";
            if (positiveShift > 10) outlook = "IMPROVING";
            else if (negativeShift > 10) outlook = "DETERIORATING";

            return new SentimentForecast
            {
                Outlook = outlook,
                PositiveShift = positiveShift,
                NegativeShift = negativeShift,
                PredictedDominantSentiment = recentSentiment.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "Neutral"
            };
        }

        private double GetSentimentChange(Dictionary<string, int> recent, Dictionary<string, int> older, string sentiment)
        {
            recent.TryGetValue(sentiment, out var recentCount);
            older.TryGetValue(sentiment, out var olderCount);

            if (olderCount == 0) return recentCount > 0 ? 100 : 0;
            return ((double)recentCount - olderCount) / olderCount * 100;
        }

        private List<EmergingTopic> DetectEmergingTopics(List<MediaPost> posts)
        {
            var recentPosts = posts.OrderByDescending(p => p.CreatedAt).Take(posts.Count / 3).ToList();
            var allWords = recentPosts.SelectMany(p =>
                p.Content.ToLower()
                    .Split(' ', '.', ',', '!', '?', ':', ';')
                    .Where(w => w.Length > 4)
            ).ToList();

            var wordFreq = allWords.GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(20)
                .Select(g => new EmergingTopic
                {
                    Keyword = g.Key,
                    Frequency = g.Count(),
                    GrowthRate = 0
                })
                .ToList();

            return wordFreq;
        }

        private int CalculateConfidence(List<MediaPost> posts)
        {
            var volumeScore = Math.Min(40, posts.Count / 10);
            var recencyScore = posts.Any(p => p.CreatedAt > DateTime.UtcNow.AddHours(-6)) ? 30 : 15;
            var consistencyScore = 25;
            return volumeScore + recencyScore + consistencyScore;
        }

        private List<string> GenerateRecommendations(List<PredictedTrend> trends, SentimentForecast sentiment)
        {
            var recommendations = new List<string>();

            var risingTrends = trends.Where(t => t.Trend == "RISING").ToList();
            if (risingTrends.Any())
            {
                recommendations.Add($"Monitor closely: {string.Join(", ", risingTrends.Take(3).Select(t => t.Category))} showing upward trend");
            }

            if (sentiment.Outlook == "DETERIORATING")
                recommendations.Add("Sentiment declining - consider proactive communication strategy");
            else if (sentiment.Outlook == "IMPROVING")
                recommendations.Add("Positive sentiment momentum - opportunity for engagement");

            if (!recommendations.Any())
                recommendations.Add("Continue standard monitoring protocols");

            return recommendations;
        }
    }

    public class TrendPrediction
    {
        public DateTime GeneratedAt { get; set; }
        public int ForecastHours { get; set; }
        public List<PredictedTrend> PredictedTrends { get; set; } = new();
        public SentimentForecast SentimentForecast { get; set; } = new();
        public List<EmergingTopic> EmergingTopics { get; set; } = new();
        public int Confidence { get; set; }
        public List<string> Recommendations { get; set; } = new();
        public string Message { get; set; } = "";
    }

    public class PredictedTrend
    {
        public string Category { get; set; } = "";
        public int CurrentVolume { get; set; }
        public int PredictedVolume { get; set; }
        public string Trend { get; set; } = "";
        public double ChangePercent { get; set; }
        public double Confidence { get; set; }
    }

    public class SentimentForecast
    {
        public string Outlook { get; set; } = "";
        public double PositiveShift { get; set; }
        public double NegativeShift { get; set; }
        public string PredictedDominantSentiment { get; set; } = "";
    }

    public class EmergingTopic
    {
        public string Keyword { get; set; } = "";
        public int Frequency { get; set; }
        public double GrowthRate { get; set; }
    }
}