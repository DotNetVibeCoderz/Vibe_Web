using System.ComponentModel.DataAnnotations;

namespace StockAnalyzer.Models;

/// <summary>
/// Application-wide user-configurable settings.
/// Stores DB connection, storage, and UI preferences.
/// </summary>
public class AppConfiguration
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string ConfigKey { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string ConfigValue { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Category { get; set; } = string.Empty; // Database, Storage, LLM, UI, General

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database connection configuration.
/// </summary>
public class DatabaseConfig
{
    /// <summary>Provider: SQLite, SqlServer, MySQL</summary>
    public string Provider { get; set; } = "SQLite";

    /// <summary>Connection string</summary>
    public string? ConnectionString { get; set; }
}

/// <summary>
/// Storage provider configuration.
/// </summary>
public class StorageConfig
{
    /// <summary>Provider: FileSystem, MinIO, S3, AzureBlob</summary>
    public string Provider { get; set; } = "FileSystem";

    /// <summary>Base path for FileSystem storage</summary>
    public string? BasePath { get; set; }

    /// <summary>MinIO/S3 endpoint</summary>
    public string? Endpoint { get; set; }

    /// <summary>Access key</summary>
    public string? AccessKey { get; set; }

    /// <summary>Secret key</summary>
    public string? SecretKey { get; set; }

    /// <summary>Bucket/container name</summary>
    public string? BucketName { get; set; }
}

/// <summary>
/// General LLM settings.
/// </summary>
public class LLMSettings
{
    /// <summary>Default provider if none specified</summary>
    public string DefaultProvider { get; set; } = "Ollama";

    /// <summary>Enable LLM-based stock analysis</summary>
    public bool EnableLLMAnalysis { get; set; } = true;

    /// <summary>Weight for technical score in overall</summary>
    public double TechnicalWeight { get; set; } = 0.35;

    /// <summary>Weight for fundamental score in overall</summary>
    public double FundamentalWeight { get; set; } = 0.35;

    /// <summary>Weight for sentiment score in overall</summary>
    public double SentimentWeight { get; set; } = 0.30;
}

/// <summary>
/// Stock data API configuration.
/// </summary>
public class StockApiConfig
{
    /// <summary>API provider: YahooFinance, AlphaVantage, IDX, etc.</summary>
    public string Provider { get; set; } = "YahooFinance";

    /// <summary>API Key if required</summary>
    public string? ApiKey { get; set; }

    /// <summary>Base URL for API</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Data refresh interval in minutes</summary>
    public int RefreshIntervalMinutes { get; set; } = 15;

    /// <summary>Whether to auto-refresh data</summary>
    public bool AutoRefresh { get; set; } = true;
}
