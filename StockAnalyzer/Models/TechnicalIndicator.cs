using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockAnalyzer.Models;

/// <summary>
/// Stores technical analysis data for a stock.
/// Includes price history, popular indicators (MA, RSI, MACD), and volume data.
/// </summary>
public class TechnicalIndicator
{
    [Key]
    public int Id { get; set; }

    /// <summary>FK to StockEmiten</summary>
    public int StockEmitenId { get; set; }

    /// <summary>Trading date</summary>
    public DateTime TradeDate { get; set; }

    // --- Price Data ---
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal AdjustedClose { get; set; }

    // --- Volume Data ---
    /// <summary>Total trading volume (in shares)</summary>
    public long Volume { get; set; }

    /// <summary>Buy volume</summary>
    public long BuyVolume { get; set; }

    /// <summary>Sell volume</summary>
    public long SellVolume { get; set; }

    /// <summary>Net foreign buy/sell</summary>
    public long ForeignNetVolume { get; set; }

    // --- Moving Averages ---
    public decimal? MA5 { get; set; }
    public decimal? MA10 { get; set; }
    public decimal? MA20 { get; set; }
    public decimal? MA50 { get; set; }
    public decimal? MA200 { get; set; }

    // --- RSI (Relative Strength Index) ---
    public decimal? RSI { get; set; }

    // --- MACD ---
    public decimal? MACD { get; set; }
    public decimal? MACDSignal { get; set; }
    public decimal? MACDHistogram { get; set; }

    // --- Bollinger Bands ---
    public decimal? BollingerUpper { get; set; }
    public decimal? BollingerMiddle { get; set; }
    public decimal? BollingerLower { get; set; }

    // --- Additional Indicators ---
    public decimal? StochasticK { get; set; }
    public decimal? StochasticD { get; set; }
    public decimal? ATR { get; set; }  // Average True Range

    // --- Bandar (Big Player) Movement ---
    public decimal? BandarAccumulation { get; set; }
    public decimal? BandarDistribution { get; set; }

    /// <summary>Data source (e.g., YahooFinance, IDX, etc.)</summary>
    [MaxLength(50)]
    public string DataSource { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(StockEmitenId))]
    public StockEmiten? StockEmiten { get; set; }
}
