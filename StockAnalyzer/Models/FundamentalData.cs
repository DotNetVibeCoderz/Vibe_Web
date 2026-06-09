using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockAnalyzer.Models;

/// <summary>
/// Stores fundamental (financial) data for a stock.
/// Contains key financial ratios and metrics for evaluating company health.
/// </summary>
public class FundamentalData
{
    [Key]
    public int Id { get; set; }

    /// <summary>FK to StockEmiten</summary>
    public int StockEmitenId { get; set; }

    /// <summary>Financial report period (e.g., Q1 2024)</summary>
    [Required, MaxLength(20)]
    public string Period { get; set; } = string.Empty;

    /// <summary>Report date</summary>
    public DateTime ReportDate { get; set; }

    // --- Profitability Ratios ---
    /// <summary>Price to Earnings Ratio</summary>
    public decimal? PER { get; set; }

    /// <summary>Earnings Per Share</summary>
    public decimal? EPS { get; set; }

    /// <summary>Return on Equity (%)</summary>
    public decimal? ROE { get; set; }

    /// <summary>Return on Assets (%)</summary>
    public decimal? ROA { get; set; }

    /// <summary>Net Profit Margin (%)</summary>
    public decimal? NetProfitMargin { get; set; }

    /// <summary>Gross Profit Margin (%)</summary>
    public decimal? GrossProfitMargin { get; set; }

    // --- Valuation Ratios ---
    /// <summary>Price to Book Value</summary>
    public decimal? PBV { get; set; }

    /// <summary>Price to Sales Ratio</summary>
    public decimal? PSR { get; set; }

    /// <summary>Enterprise Value to EBITDA</summary>
    public decimal? EVEBITDA { get; set; }

    /// <summary>Dividend Yield (%)</summary>
    public decimal? DividendYield { get; set; }

    // --- Solvency Ratios ---
    /// <summary>Debt to Equity Ratio</summary>
    public decimal? DER { get; set; }

    /// <summary>Current Ratio</summary>
    public decimal? CurrentRatio { get; set; }

    /// <summary>Interest Coverage Ratio</summary>
    public decimal? InterestCoverage { get; set; }

    // --- Growth Metrics ---
    public decimal? RevenueGrowth { get; set; }
    public decimal? EarningsGrowth { get; set; }

    // --- Cash Flow ---
    public decimal? OperatingCashFlow { get; set; }
    public decimal? FreeCashFlow { get; set; }
    public decimal? CashFlowPerShare { get; set; }

    // --- Balance Sheet (in billions IDR) ---
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? TotalEquity { get; set; }
    public decimal? Revenue { get; set; }
    public decimal? NetIncome { get; set; }

    [MaxLength(50)]
    public string DataSource { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(StockEmitenId))]
    public StockEmiten? StockEmiten { get; set; }
}
