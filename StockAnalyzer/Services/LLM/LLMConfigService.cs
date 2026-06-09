using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Data;
using StockAnalyzer.Models;

namespace StockAnalyzer.Services.LLM;

/// <summary>
/// Configuration service for LLM settings.
/// Syncs between appsettings.json and database.
/// </summary>
public class LLMConfigService : ILLMConfigService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LLMConfigService> _logger;

    public LLMConfigService(AppDbContext db, IConfiguration configuration, ILogger<LLMConfigService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Sync LLM configurations from appsettings.json to the database.
    /// This ensures the database has the latest config from appsettings.
    /// </summary>
    public async Task SyncConfigFromAppSettingsAsync()
    {
        try
        {
            var providersSection = _configuration.GetSection("LLM:Providers");
            if (!providersSection.Exists()) return;

            foreach (var child in providersSection.GetChildren())
            {
                var providerName = child.Key;
                var existing = await _db.LLMProviderConfigs
                    .FirstOrDefaultAsync(p => p.ProviderName == providerName);

                if (existing != null)
                {
                    // Update existing
                    existing.ApiKey = child["ApiKey"] ?? existing.ApiKey;
                    existing.ApiBaseUrl = child["ApiBaseUrl"] ?? existing.ApiBaseUrl;
                    existing.ModelName = child["ModelName"] ?? existing.ModelName;
                    existing.FallbackModelName = child["FallbackModelName"] ?? existing.FallbackModelName;
                    existing.MaxTokens = int.TryParse(child["MaxTokens"], out var mt) ? mt : existing.MaxTokens;
                    existing.Temperature = double.TryParse(child["Temperature"], out var temp) ? temp : existing.Temperature;
                    existing.IsEnabled = bool.TryParse(child["IsEnabled"], out var enabled) ? enabled : existing.IsEnabled;
                    existing.TimeoutSeconds = int.TryParse(child["TimeoutSeconds"], out var ts) ? ts : existing.TimeoutSeconds;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("LLM configurations synced from appsettings.json");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync LLM configs from appsettings: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Update a provider configuration.
    /// </summary>
    public async Task UpdateProviderConfigAsync(LLMProviderConfig config)
    {
        var existing = await _db.LLMProviderConfigs.FindAsync(config.Id);
        if (existing != null)
        {
            existing.ApiKey = config.ApiKey;
            existing.ApiBaseUrl = config.ApiBaseUrl;
            existing.ModelName = config.ModelName;
            existing.FallbackModelName = config.FallbackModelName;
            existing.MaxTokens = config.MaxTokens;
            existing.Temperature = config.Temperature;
            existing.IsEnabled = config.IsEnabled;
            existing.TimeoutSeconds = config.TimeoutSeconds;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.LLMProviderConfigs.Add(config);
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Get configuration for a specific provider.
    /// </summary>
    public async Task<LLMProviderConfig?> GetProviderConfigAsync(string providerName)
    {
        return await _db.LLMProviderConfigs
            .FirstOrDefaultAsync(p => p.ProviderName == providerName);
    }

    /// <summary>
    /// Get all provider configurations.
    /// </summary>
    public async Task<List<LLMProviderConfig>> GetAllProviderConfigsAsync()
    {
        return await _db.LLMProviderConfigs
            .OrderBy(p => p.Priority)
            .ToListAsync();
    }
}
