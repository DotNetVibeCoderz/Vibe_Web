using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Data;
using StockAnalyzer.Models;

namespace StockAnalyzer.Services.StockData;

/// <summary>
/// Implementation of IStockDataService for managing stock data.
/// </summary>
public class StockDataService : IStockDataService
{
    private readonly AppDbContext _db;
    private readonly ILogger<StockDataService> _logger;

    public StockDataService(AppDbContext db, ILogger<StockDataService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== Stock Emiten ====================

    public async Task<List<StockEmiten>> GetAllStocksAsync()
    {
        return await _db.StockEmitens
            .Where(s => s.IsActive)
            .OrderBy(s => s.StockCode)
            .ToListAsync();
    }

    public async Task<List<StockEmiten>> GetStocksBySectorAsync(string sector)
    {
        return await _db.StockEmitens
            .Where(s => s.IsActive && s.Sector == sector)
            .OrderBy(s => s.MarketCap)
            .ToListAsync();
    }

    public async Task<StockEmiten?> GetStockByCodeAsync(string stockCode)
    {
        return await _db.StockEmitens
            .FirstOrDefaultAsync(s => s.StockCode == stockCode.ToUpper());
    }

    public async Task<StockEmiten?> GetStockByIdAsync(int id)
    {
        return await _db.StockEmitens.FindAsync(id);
    }

    public async Task<List<string>> GetAllSectorsAsync()
    {
        return await _db.StockEmitens
            .Where(s => s.IsActive)
            .Select(s => s.Sector)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    public async Task<StockEmiten> AddOrUpdateStockAsync(StockEmiten stock)
    {
        var existing = await _db.StockEmitens
            .FirstOrDefaultAsync(s => s.StockCode == stock.StockCode);

        if (existing != null)
        {
            existing.CompanyName = stock.CompanyName;
            existing.Sector = stock.Sector;
            existing.SubSector = stock.SubSector;
            existing.Description = stock.Description;
            existing.ListedShares = stock.ListedShares;
            existing.MarketCap = stock.MarketCap;
            existing.CurrentPrice = stock.CurrentPrice;
            existing.ChangePercent = stock.ChangePercent;
            existing.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            stock.LastUpdated = DateTime.UtcNow;
            _db.StockEmitens.Add(stock);
        }

        await _db.SaveChangesAsync();
        return existing ?? stock;
    }

    // ==================== Technical Data ====================

    public async Task<List<TechnicalIndicator>> GetTechnicalHistoryAsync(int stockId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.TechnicalIndicators
            .Where(t => t.StockEmitenId == stockId);

        if (from.HasValue)
            query = query.Where(t => t.TradeDate >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.TradeDate <= to.Value);

        return await query.OrderBy(t => t.TradeDate).ToListAsync();
    }

    public async Task<TechnicalIndicator?> GetLatestTechnicalAsync(int stockId)
    {
        return await _db.TechnicalIndicators
            .Where(t => t.StockEmitenId == stockId)
            .OrderByDescending(t => t.TradeDate)
            .FirstOrDefaultAsync();
    }

    // ==================== Fundamental Data ====================

    public async Task<List<FundamentalData>> GetFundamentalHistoryAsync(int stockId)
    {
        return await _db.FundamentalData
            .Where(f => f.StockEmitenId == stockId)
            .OrderByDescending(f => f.ReportDate)
            .ToListAsync();
    }

    public async Task<FundamentalData?> GetLatestFundamentalAsync(int stockId)
    {
        return await _db.FundamentalData
            .Where(f => f.StockEmitenId == stockId)
            .OrderByDescending(f => f.ReportDate)
            .FirstOrDefaultAsync();
    }

    // ==================== Sentiment Data ====================

    public async Task<List<SentimentData>> GetSentimentDataAsync(int stockId, int limit = 20)
    {
        return await _db.SentimentData
            .Where(s => s.StockEmitenId == stockId)
            .OrderByDescending(s => s.PublishedDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<SectorSentiment>> GetSectorSentimentsAsync()
    {
        return await _db.SectorSentiments
            .OrderByDescending(s => s.AnalysisDate)
            .ThenBy(s => s.Sector)
            .ToListAsync();
    }

    public async Task<SectorSentiment?> GetSectorSentimentAsync(string sector)
    {
        return await _db.SectorSentiments
            .Where(s => s.Sector == sector)
            .OrderByDescending(s => s.AnalysisDate)
            .FirstOrDefaultAsync();
    }

    // ==================== Market Overview ====================

    public async Task<int> GetTotalStocksAsync()
    {
        return await _db.StockEmitens.CountAsync(s => s.IsActive);
    }

    public async Task<decimal> GetAverageMarketSentimentAsync()
    {
        var sentiments = await _db.SentimentData
            .Where(s => s.PublishedDate >= DateTime.UtcNow.AddDays(-7))
            .ToListAsync();

        if (sentiments.Count == 0) return 0;

        return (decimal)sentiments.Average(s => s.SentimentScore);
    }

    public async Task<Dictionary<string, int>> GetStocksBySectorCountAsync()
    {
        return await _db.StockEmitens
            .Where(s => s.IsActive)
            .GroupBy(s => s.Sector)
            .Select(g => new { Sector = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Sector, x => x.Count);
    }
}
