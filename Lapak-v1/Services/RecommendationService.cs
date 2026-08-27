using Lapak.Models;
using Lapak.Models.Configurations;
using Lapak.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lapak.Services;

/// <summary>
/// AI Recommendation Engine - combines collaborative filtering and content-based approaches
/// </summary>
public interface IRecommendationService
{
    Task<List<Product>> GetRecommendationsAsync(Guid userId, int count = 10);
    Task<List<Product>> GetSimilarProductsAsync(Guid productId, int count = 6);
    Task<List<Product>> GetTrendingProductsAsync(int count = 10);
    Task<List<Product>> GetPersonalizedRecommendationsAsync(Guid userId, int count = 10);
}

public class RecommendationService : IRecommendationService
{
    private readonly LapakDbContext _db;
    private readonly RecommendationConfig _config;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        LapakDbContext db,
        IOptions<RecommendationConfig> config,
        ILogger<RecommendationService> logger)
    {
        _db = db;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<List<Product>> GetRecommendationsAsync(Guid userId, int count = 10)
    {
        var collaborative = await GetCollaborativeRecommendationsAsync(userId);
        var contentBased = await GetContentBasedRecommendationsAsync(userId);

        var scoredProducts = new Dictionary<Guid, double>();

        foreach (var (productId, score) in collaborative)
            scoredProducts[productId] = score * _config.CollaborativeFilteringWeight;

        foreach (var (productId, score) in contentBased)
        {
            if (scoredProducts.ContainsKey(productId))
                scoredProducts[productId] += score * _config.ContentBasedWeight;
            else
                scoredProducts[productId] = score * _config.ContentBasedWeight;
        }

        var topProductIds = scoredProducts
            .OrderByDescending(x => x.Value)
            .Take(count)
            .Select(x => x.Key)
            .ToList();

        if (topProductIds.Count == 0)
            return await GetTrendingProductsAsync(count);

        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Store)
            .Where(p => topProductIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        return topProductIds
            .Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Cast<Product>()
            .ToList();
    }

    private async Task<Dictionary<Guid, double>> GetCollaborativeRecommendationsAsync(Guid userId)
    {
        var results = new Dictionary<Guid, double>();

        try
        {
            var userProductIds = await _db.OrderItems
                .Where(oi => oi.Order.UserId == userId && oi.Order.Status == "Completed")
                .Select(oi => oi.ProductId)
                .Distinct()
                .ToListAsync();

            if (userProductIds.Count == 0) return results;

            var similarUserIds = await _db.OrderItems
                .Where(oi => userProductIds.Contains(oi.ProductId) && oi.Order.UserId != userId)
                .Select(oi => oi.Order.UserId)
                .Distinct()
                .Take(100)
                .ToListAsync();

            var recommendedProducts = await _db.OrderItems
                .Where(oi => similarUserIds.Contains(oi.Order.UserId) &&
                            !userProductIds.Contains(oi.ProductId) &&
                            oi.Order.Status == "Completed")
                .GroupBy(oi => oi.ProductId)
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(50)
                .ToListAsync();

            var maxCount = recommendedProducts.FirstOrDefault()?.Count ?? 1;
            foreach (var item in recommendedProducts)
                results[item.ProductId] = (double)item.Count / maxCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Collaborative filtering failed");
        }

        return results;
    }

    private async Task<Dictionary<Guid, double>> GetContentBasedRecommendationsAsync(Guid userId)
    {
        var results = new Dictionary<Guid, double>();

        try
        {
            var purchasedCategoryIds = await _db.OrderItems
                .Where(oi => oi.Order.UserId == userId)
                .Select(oi => oi.Product.CategoryId)
                .Distinct()
                .ToListAsync();

            var cartCategoryIds = await _db.CartItems
                .Where(ci => ci.UserId == userId)
                .Select(ci => ci.Product.CategoryId)
                .Distinct()
                .ToListAsync();

            var allCategoryIds = purchasedCategoryIds.Union(cartCategoryIds).Distinct().ToList();

            if (allCategoryIds.Count == 0)
            {
                var popular = await _db.Products
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.SoldCount)
                    .Take(20)
                    .Select(p => new { p.Id, p.SoldCount })
                    .ToListAsync();

                var maxSold = popular.FirstOrDefault()?.SoldCount ?? 1;
                foreach (var p in popular)
                    results[p.Id] = (double)p.SoldCount / maxSold;

                return results;
            }

            var categoryProducts = await _db.Products
                .Where(p => p.IsActive && allCategoryIds.Contains(p.CategoryId))
                .OrderByDescending(p => p.AverageRating)
                .ThenByDescending(p => p.SoldCount)
                .Take(50)
                .Select(p => new { p.Id, p.AverageRating, p.SoldCount })
                .ToListAsync();

            foreach (var p in categoryProducts)
                results[p.Id] = (p.AverageRating / 5.0 * 0.7) + (Math.Min(p.SoldCount, 100) / 100.0 * 0.3);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Content-based recommendations failed");
        }

        return results;
    }

    public async Task<List<Product>> GetSimilarProductsAsync(Guid productId, int count = 6)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return new List<Product>();

        return await _db.Products
            .Include(p => p.Store)
            .Where(p => p.Id != productId &&
                       p.CategoryId == product.CategoryId &&
                       p.IsActive)
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.SoldCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Product>> GetTrendingProductsAsync(int count = 10)
    {
        return await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Store)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.SoldCount)
            .ThenByDescending(p => p.ViewCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Product>> GetPersonalizedRecommendationsAsync(Guid userId, int count = 10)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return await GetTrendingProductsAsync(count);

        var recommendations = await GetRecommendationsAsync(userId, count);

        if (recommendations.Count < count)
        {
            var additional = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Store)
                .Where(p => p.IsActive && !recommendations.Select(r => r.Id).Contains(p.Id))
                .OrderByDescending(p => p.AverageRating)
                .Take(count - recommendations.Count)
                .ToListAsync();

            recommendations.AddRange(additional);
        }

        return recommendations;
    }
}
