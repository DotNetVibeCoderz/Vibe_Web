using FamilyTree.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class StatsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public StatsService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<double> GetAverageLifespanAsync(int treeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var people = await context.People
            .Where(p => p.FamilyTreeEntityId == treeId && p.DeathDate.HasValue)
            .ToListAsync();

        if (!people.Any())
        {
            return 0;
        }

        return people.Average(p => (p.DeathDate!.Value - p.BirthDate).TotalDays / 365.25);
    }

    public async Task<Dictionary<string, int>> GetGeographicDistributionAsync(int treeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.People
            .Where(p => p.FamilyTreeEntityId == treeId)
            .GroupBy(p => p.Location ?? "Unknown")
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetOccupationTrendsAsync(int treeId)
    {
        // Placeholder data; extend with occupation field in future
        return await Task.FromResult(new Dictionary<string, int>
        {
            { "Teknologi", 3 },
            { "Kesehatan", 2 },
            { "Pendidikan", 1 }
        });
    }
}
