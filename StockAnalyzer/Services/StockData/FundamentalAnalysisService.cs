using StockAnalyzer.Models;

namespace StockAnalyzer.Services.StockData;

/// <summary>
/// Fundamental analysis service for calculating and assessing financial ratios.
/// </summary>
public class FundamentalAnalysisService : IFundamentalAnalysisService
{
    // ==================== Ratio Calculations ====================

    /// <summary>Price to Earnings Ratio</summary>
    public decimal CalculatePER(decimal price, decimal eps)
    {
        if (eps == 0) return 0;
        return price / eps;
    }

    /// <summary>Price to Book Value</summary>
    public decimal CalculatePBV(decimal price, decimal bookValuePerShare)
    {
        if (bookValuePerShare == 0) return 0;
        return price / bookValuePerShare;
    }

    /// <summary>Debt to Equity Ratio</summary>
    public decimal CalculateDER(decimal totalDebt, decimal totalEquity)
    {
        if (totalEquity == 0) return 0;
        return totalDebt / totalEquity;
    }

    /// <summary>Return on Equity (percentage)</summary>
    public decimal CalculateROE(decimal netIncome, decimal totalEquity)
    {
        if (totalEquity == 0) return 0;
        return (netIncome / totalEquity) * 100;
    }

    /// <summary>Earnings Per Share</summary>
    public decimal CalculateEPS(decimal netIncome, decimal outstandingShares)
    {
        if (outstandingShares == 0) return 0;
        return netIncome / outstandingShares;
    }

    /// <summary>Current Ratio</summary>
    public decimal CalculateCurrentRatio(decimal currentAssets, decimal currentLiabilities)
    {
        if (currentLiabilities == 0) return 0;
        return currentAssets / currentLiabilities;
    }

    // ==================== Ratio Assessments ====================

    /// <summary>
    /// Assess PER value. Lower PER generally means undervalued (cheaper).
    /// </summary>
    public string AssessPER(decimal? per)
    {
        if (!per.HasValue || per.Value == 0) return "N/A";
        return per.Value switch
        {
            < 5 => "🟢 Very Undervalued",
            < 10 => "🟢 Undervalued",
            < 15 => "🟡 Fair Value",
            < 25 => "🟠 Slightly Overvalued",
            _ => "🔴 Overvalued"
        };
    }

    /// <summary>
    /// Assess PBV value. Lower PBV means closer to book value.
    /// </summary>
    public string AssessPBV(decimal? pbv)
    {
        if (!pbv.HasValue || pbv.Value == 0) return "N/A";
        return pbv.Value switch
        {
            < 1 => "🟢 Below Book Value",
            < 2 => "🟡 Fair Value",
            < 3 => "🟠 Above Book Value",
            _ => "🔴 Overpriced"
        };
    }

    /// <summary>
    /// Assess DER - higher means more debt/risk.
    /// </summary>
    public string AssessDER(decimal? der)
    {
        if (!der.HasValue) return "N/A";
        return der.Value switch
        {
            < 0.5m => "🟢 Very Low Debt",
            < 1.0m => "🟡 Moderate Debt",
            < 2.0m => "🟠 High Debt",
            _ => "🔴 Very High Debt"
        };
    }

    /// <summary>
    /// Assess ROE - higher is better.
    /// </summary>
    public string AssessROE(decimal? roe)
    {
        if (!roe.HasValue) return "N/A";
        return roe.Value switch
        {
            < 0 => "🔴 Negative",
            < 5 => "🟠 Low",
            < 10 => "🟡 Moderate",
            < 20 => "🟢 Good",
            _ => "🟢 Excellent"
        };
    }

    /// <summary>
    /// Assess revenue and earnings growth.
    /// </summary>
    public string AssessGrowth(decimal? revenueGrowth, decimal? earningsGrowth)
    {
        var avgGrowth = ((revenueGrowth ?? 0) + (earningsGrowth ?? 0)) / 2;
        return avgGrowth switch
        {
            < 0 => "🔴 Declining",
            < 5 => "🟠 Slow Growth",
            < 15 => "🟡 Moderate Growth",
            < 30 => "🟢 Strong Growth",
            _ => "🟢 Very Strong Growth"
        };
    }

    // ==================== Fundamental Score ====================

    /// <summary>
    /// Calculate a fundamental score (0-100) based on key ratios.
    /// </summary>
    public double CalculateFundamentalScore(FundamentalData data)
    {
        double score = 50;

        // --- PER (lower is better up to a point) ---
        if (data.PER.HasValue)
        {
            if (data.PER.Value > 0 && data.PER.Value < 10) score += 15;
            else if (data.PER.Value >= 10 && data.PER.Value < 15) score += 10;
            else if (data.PER.Value >= 15 && data.PER.Value < 20) score += 5;
            else if (data.PER.Value >= 20 && data.PER.Value < 30) score += 0;
            else if (data.PER.Value >= 30) score -= 5;
            else if (data.PER.Value < 0) score -= 10; // Negative EPS
        }

        // --- PBV ---
        if (data.PBV.HasValue)
        {
            if (data.PBV.Value < 1) score += 10;
            else if (data.PBV.Value < 2) score += 5;
            else if (data.PBV.Value < 3) score += 0;
            else score -= 5;
        }

        // --- DER ---
        if (data.DER.HasValue)
        {
            if (data.DER.Value < 0.5m) score += 10;
            else if (data.DER.Value < 1.0m) score += 5;
            else if (data.DER.Value < 2.0m) score += 0;
            else score -= 10;
        }

        // --- ROE ---
        if (data.ROE.HasValue)
        {
            if (data.ROE.Value > 20) score += 15;
            else if (data.ROE.Value > 10) score += 10;
            else if (data.ROE.Value > 5) score += 5;
            else if (data.ROE.Value < 0) score -= 10;
        }

        // --- Growth ---
        var avgGrowth = ((data.RevenueGrowth ?? 0) + (data.EarningsGrowth ?? 0)) / 2;
        if (avgGrowth > 20) score += 10;
        else if (avgGrowth > 10) score += 5;
        else if (avgGrowth < 0) score -= 10;

        // --- Current Ratio ---
        if (data.CurrentRatio.HasValue)
        {
            if (data.CurrentRatio.Value > 2) score += 5;
            else if (data.CurrentRatio.Value < 1) score -= 5;
        }

        // --- Free Cash Flow ---
        if (data.FreeCashFlow.HasValue && data.FreeCashFlow.Value > 0) score += 5;
        else if (data.FreeCashFlow.HasValue && data.FreeCashFlow.Value < 0) score -= 5;

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// Generate a summary of key fundamental metrics.
    /// </summary>
    public string GetFundamentalSummary(FundamentalData data)
    {
        var summaries = new List<string>();

        if (data.PER.HasValue && data.PER > 0)
            summaries.Add($"PER: {data.PER:F2}x ({AssessPER(data.PER)})");

        if (data.PBV.HasValue && data.PBV > 0)
            summaries.Add($"PBV: {data.PBV:F2}x ({AssessPBV(data.PBV)})");

        if (data.DER.HasValue)
            summaries.Add($"DER: {data.DER:F2}x ({AssessDER(data.DER)})");

        if (data.ROE.HasValue)
            summaries.Add($"ROE: {data.ROE:F1}% ({AssessROE(data.ROE)})");

        if (data.EPS.HasValue)
            summaries.Add($"EPS: {data.EPS:F2}");

        return string.Join(" | ", summaries);
    }
}
