using Microsoft.ML.Data;

namespace MediaMonitoring.Models.ML
{
    /// <summary>
    /// Model input untuk ML.NET Sentiment Analysis
    /// </summary>
    public class SentimentData
    {
        [LoadColumn(0)]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model output untuk prediksi sentimen
    /// </summary>
    public class SentimentPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedSentiment { get; set; } // true = Positive, false = Negative
        
        [ColumnName("Score")]
        public float Probability { get; set; }
    }

    /// <summary>
    /// Model untuk multi-class sentiment (Positive, Negative, Neutral)
    /// </summary>
    public class SentimentIssue
    {
        [LoadColumn(0)]
        public string Text { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string Sentiment { get; set; } = string.Empty; // Positive, Negative, Neutral
    }

    public class SentimentPredictionMulti
    {
        [ColumnName("PredictedLabel")]
        public string PredictedSentiment { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[] Score { get; set; } = Array.Empty<float>();
    }
}