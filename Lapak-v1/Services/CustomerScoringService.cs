using Lapak.Models;
using Lapak.Models.Configurations;
using Lapak.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lapak.Services;

/// <summary>
/// Customer Scoring Service - calculates and manages customer tiers
/// </summary>
public interface ICustomerScoringService
{
    Task<int> CalculateScoreAsync(Guid userId);
    Task<string> DetermineTierAsync(int score);
    Task UpdateCustomerScoreAsync(Guid userId);
    Task<List<User>> GetCustomersByTierAsync(string tier);
    Task<Dictionary<string, int>> GetTierDistributionAsync();
    Task RecalculateAllScoresAsync();
}

public class CustomerScoringService : ICustomerScoringService
{
    private readonly LapakDbContext _db;
    private readonly CustomerScoringConfig _config;
    private readonly ILogger<CustomerScoringService> _logger;

    public CustomerScoringService(
        LapakDbContext db,
        IOptions<CustomerScoringConfig> config,
        ILogger<CustomerScoringService> logger)
    {
        _db = db;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<int> CalculateScoreAsync(Guid userId)
    {
        try
        {
            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId && o.Status == "Completed")
                .ToListAsync();

            if (orders.Count == 0) return 0;

            var transactionCount = orders.Count;
            var transactionCountScore = Math.Min(transactionCount * 10, 100) * _config.TransactionCountWeight;

            var totalValue = orders.Sum(o => o.GrandTotal);
            var transactionValueScore = Math.Min((double)(totalValue / 10000), 100) * _config.TransactionValueWeight;

            var categoryIds = orders
                .SelectMany(o => o.OrderItems)
                .Select(oi => oi.Product.CategoryId)
                .Distinct()
                .Count();
            var categoryScore = Math.Min(categoryIds * 20, 100) * _config.CategoryDiversityWeight;

            return (int)(transactionCountScore + transactionValueScore + categoryScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating score for user {UserId}", userId);
            return 0;
        }
    }

    public Task<string> DetermineTierAsync(int score)
    {
        string tier = score >= _config.PlatinumThreshold ? "Platinum"
            : score >= _config.GoldThreshold ? "Gold"
            : score >= _config.SilverThreshold ? "Silver"
            : "Bronze";

        return Task.FromResult(tier);
    }

    public async Task UpdateCustomerScoreAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return;

        var score = await CalculateScoreAsync(userId);
        var tier = await DetermineTierAsync(score);

        user.Score = score;
        user.Tier = tier;
        user.TotalTransactions = await _db.Orders.CountAsync(o => o.UserId == userId && o.Status == "Completed");
        user.TotalTransactionValue = await _db.Orders
            .Where(o => o.UserId == userId && o.Status == "Completed")
            .SumAsync(o => o.GrandTotal);

        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated score for user {UserId}: {Score} ({Tier})", userId, score, tier);
    }

    public async Task<List<User>> GetCustomersByTierAsync(string tier)
    {
        return await _db.Users
            .Where(u => u.Tier == tier && u.IsActive)
            .OrderByDescending(u => u.Score)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetTierDistributionAsync()
    {
        return await _db.Users
            .Where(u => u.IsActive)
            .GroupBy(u => u.Tier)
            .Select(g => new { Tier = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Tier, x => x.Count);
    }

    public async Task RecalculateAllScoresAsync()
    {
        var users = await _db.Users.Where(u => u.IsActive).ToListAsync();
        foreach (var user in users)
        {
            await UpdateCustomerScoreAsync(user.Id);
        }
        _logger.LogInformation("Recalculated scores for {Count} users", users.Count);
    }
}
