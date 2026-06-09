using System.ComponentModel.DataAnnotations;

namespace StockAnalyzer.Models;

/// <summary>
/// Configuration for LLM providers and model assignments.
/// Supports OpenAI, Gemini, Anthropic, Ollama, and OpenAI-compatible providers.
/// </summary>
public class LLMProviderConfig
{
    [Key]
    public int Id { get; set; }

    /// <summary>Provider name: OpenAI, Gemini, Anthropic, Ollama, OpenAICompatible</summary>
    [Required, MaxLength(50)]
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>Display name for UI</summary>
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>API Key</summary>
    [MaxLength(500)]
    public string? ApiKey { get; set; }

    /// <summary>API Base URL (for Ollama, OpenAI-compatible, or custom endpoints)</summary>
    [MaxLength(500)]
    public string? ApiBaseUrl { get; set; }

    /// <summary>Model name (e.g., gpt-4o, gemini-2.0-flash, claude-3-sonnet, llama3)</summary>
    [MaxLength(100)]
    public string? ModelName { get; set; }

    /// <summary>Alternative model name for fallback</summary>
    [MaxLength(100)]
    public string? FallbackModelName { get; set; }

    /// <summary>Maximum tokens for response</summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>Temperature (0-2), controls randomness</summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>Whether this provider is enabled</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Request timeout in seconds</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Priority order (lower = higher priority)</summary>
    public int Priority { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Maps analysis types to specific LLM providers/models.
/// Allows different models for different analysis tasks.
/// </summary>
public class LLMAnalysisMapping
{
    [Key]
    public int Id { get; set; }

    /// <summary>Analysis type: TechnicalReview, FundamentalReview, SentimentAnalysis, StockRecommendation</summary>
    [Required, MaxLength(50)]
    public string AnalysisType { get; set; } = string.Empty;

    /// <summary>FK to LLMProviderConfig</summary>
    public int LLMProviderConfigId { get; set; }

    /// <summary>Custom system prompt for this analysis type</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Whether this mapping is active</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public LLMProviderConfig? LLMProviderConfig { get; set; }
}
