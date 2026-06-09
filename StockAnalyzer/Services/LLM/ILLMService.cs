namespace StockAnalyzer.Services.LLM;

/// <summary>
/// Request object for LLM analysis.
/// </summary>
public class LLMRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
}

/// <summary>
/// Response object from LLM analysis.
/// </summary>
public class LLMResponse
{
    public string Content { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public int TokensUsed { get; set; }
}

/// <summary>
/// Main LLM service interface.
/// </summary>
public interface ILLMService
{
    /// <summary>Send a prompt to the default LLM provider</summary>
    Task<LLMResponse> AnalyzeAsync(LLMRequest request);

    /// <summary>Send a prompt to a specific provider</summary>
    Task<LLMResponse> AnalyzeWithProviderAsync(LLMRequest request, string providerName);

    /// <summary>Get stock technical review from LLM</summary>
    Task<LLMResponse> GetTechnicalReviewAsync(string stockCode, string technicalData);

    /// <summary>Get stock fundamental review from LLM</summary>
    Task<LLMResponse> GetFundamentalReviewAsync(string stockCode, string fundamentalData);

    /// <summary>Get sentiment analysis from LLM</summary>
    Task<LLMResponse> GetSentimentAnalysisAsync(string stockCode, string newsData);

    /// <summary>Get comprehensive stock recommendation from LLM</summary>
    Task<LLMResponse> GetStockRecommendationAsync(string stockCode, string technicalData,
        string fundamentalData, string sentimentData);

    /// <summary>Check if any LLM provider is available</summary>
    Task<bool> IsAnyProviderAvailableAsync();

    /// <summary>Get list of available providers</summary>
    Task<List<string>> GetAvailableProvidersAsync();
}

/// <summary>
/// Individual LLM provider interface.
/// Each provider (OpenAI, Gemini, Anthropic, Ollama) implements this.
/// </summary>
public interface ILLMProvider
{
    string ProviderName { get; }
    Task<LLMResponse> SendMessageAsync(LLMRequest request);
    Task<bool> IsAvailableAsync();
}

/// <summary>
/// Factory for creating LLM provider instances based on configuration.
/// </summary>
public interface ILLMProviderFactory
{
    ILLMProvider? GetProvider(string providerName);
    ILLMProvider? GetDefaultProvider();
    ILLMProvider? GetProviderForAnalysisType(string analysisType);
    List<ILLMProvider> GetAllProviders();
}

/// <summary>
/// Configuration service for LLM settings.
/// </summary>
public interface ILLMConfigService
{
    Task SyncConfigFromAppSettingsAsync();
    Task UpdateProviderConfigAsync(Models.LLMProviderConfig config);
    Task<Models.LLMProviderConfig?> GetProviderConfigAsync(string providerName);
    Task<List<Models.LLMProviderConfig>> GetAllProviderConfigsAsync();
}
