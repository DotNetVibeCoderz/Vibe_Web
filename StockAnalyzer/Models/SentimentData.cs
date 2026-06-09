using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockAnalyzer.Models;

/// <summary>
/// Stores news sentiment analysis data for stocks.
/// Used to gauge market sentiment and news impact.
/// </summary>
public class SentimentData
{
    [Key]
    public int Id { get; set; }

    /// <summary>FK to StockEmiten</summary>
    public int StockEmitenId { get; set; }

    /// <summary>News headline</summary>
    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>News content/summary</summary>
    public string? Content { get; set; }

    /// <summary>News source URL</summary>
    [MaxLength(1000)]
    public string? SourceUrl { get; set; }

    /// <summary>News publisher name</summary>
    [MaxLength(200)]
    public string? Publisher { get; set; }

    /// <summary>Publication date</summary>
    public DateTime PublishedDate { get; set; }

    /// <summary>Sentiment score: -1.0 (very negative) to +1.0 (very positive)</summary>
    public double SentimentScore { get; set; }

    /// <summary>Sentiment label</summary>
    [MaxLength(20)]
    public string SentimentLabel { get; set; } = "neutral"; // positive, negative, neutral

    /// <summary>Confidence level of sentiment analysis (0-1)</summary>
    public double SentimentConfidence { get; set; }

    /// <summary>Related sector for clustering</summary>
    [MaxLength(100)]
    public string? RelatedSector { get; set; }

    /// <summary>Keywords extracted from news</summary>
    public string? Keywords { get; set; }

    /// <summary>Whether this news has been analyzed by LLM</summary>
    public bool IsAnalyzed { get; set; }

    /// <summary>LLM analysis result (if any)</summary>
    public string? LLMAnalysis { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(StockEmitenId))]
    public StockEmiten? StockEmiten { get; set; }
}

/// <summary>
/// Sector-level sentiment aggregation for clustering news by sector.
/// </summary>
public class SectorSentiment
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Sector { get; set; } = string.Empty;

    /// <summary>Average sentiment score for the sector</summary>
    public double AverageSentiment { get; set; }

    /// <summary>Number of news articles analyzed</summary>
    public int NewsCount { get; set; }

    /// <summary>Positive news count</summary>
    public int PositiveCount { get; set; }

    /// <summary>Negative news count</summary>
    public int NegativeCount { get; set; }

    /// <summary>Neutral news count</summary>
    public int NeutralCount { get; set; }

    /// <summary>Analysis date</summary>
    public DateTime AnalysisDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
