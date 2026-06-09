using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockAnalyzer.Models;

/// <summary>
/// Represents a stock/company listed on the exchange.
/// Contains basic information about the company and its stock code.
/// </summary>
public class StockEmiten
{
    [Key]
    public int Id { get; set; }

    /// <summary>Stock ticker code (e.g., BBCA, TLKM, ASII)</summary>
    [Required, MaxLength(10)]
    public string StockCode { get; set; } = string.Empty;

    /// <summary>Full company name</summary>
    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Industry sector (e.g., Banking, Technology, Mining)</summary>
    [MaxLength(100)]
    public string Sector { get; set; } = string.Empty;

    /// <summary>Sub-sector for granular classification</summary>
    [MaxLength(100)]
    public string SubSector { get; set; } = string.Empty;

    /// <summary>Company description</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Number of listed shares (in billions)</summary>
    public decimal ListedShares { get; set; }

    /// <summary>Market capitalization (in billions IDR)</summary>
    public decimal MarketCap { get; set; }

    /// <summary>IPO date</summary>
    public DateTime? IpoDate { get; set; }

    /// <summary>Current stock price</summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>Price change percentage</summary>
    public decimal ChangePercent { get; set; }

    /// <summary>Last updated timestamp</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>Whether this stock is actively tracked</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<TechnicalIndicator> TechnicalIndicators { get; set; } = new List<TechnicalIndicator>();
    public ICollection<FundamentalData> FundamentalData { get; set; } = new List<FundamentalData>();
    public ICollection<SentimentData> SentimentData { get; set; } = new List<SentimentData>();
    public ICollection<StockRecommendation> Recommendations { get; set; } = new List<StockRecommendation>();
}
