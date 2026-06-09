using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StockAnalyzer.Services.LLM;

/// <summary>
/// Factory for creating and managing LLM provider instances.
/// Uses DI to resolve providers by name.
/// </summary>
public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly Dictionary<string, ILLMProvider> _providers;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LLMProviderFactory> _logger;

    public LLMProviderFactory(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<LLMProviderFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Build provider dictionary using DI
        _providers = new Dictionary<string, ILLMProvider>(StringComparer.OrdinalIgnoreCase)
        {
            ["OpenAI"] = serviceProvider.GetRequiredService<OpenAIProvider>(),
            ["Gemini"] = serviceProvider.GetRequiredService<GeminiProvider>(),
            ["Anthropic"] = serviceProvider.GetRequiredService<AnthropicProvider>(),
            ["Ollama"] = serviceProvider.GetRequiredService<OllamaProvider>(),
            ["OpenAICompatible"] = serviceProvider.GetRequiredService<OpenAICompatibleProvider>()
        };
    }

    /// <summary>
    /// Get a specific provider by name.
    /// </summary>
    public ILLMProvider? GetProvider(string providerName)
    {
        if (_providers.TryGetValue(providerName, out var provider))
        {
            return provider;
        }
        _logger.LogWarning("Provider '{ProviderName}' not found", providerName);
        return null;
    }

    /// <summary>
    /// Get the default provider as configured.
    /// </summary>
    public ILLMProvider? GetDefaultProvider()
    {
        var defaultProvider = _configuration["LLM:DefaultProvider"] ?? "Ollama";
        return GetProvider(defaultProvider);
    }

    /// <summary>
    /// Get the provider configured for a specific analysis type.
    /// Falls back to default provider if not configured.
    /// </summary>
    public ILLMProvider? GetProviderForAnalysisType(string analysisType)
    {
        var providerName = _configuration[$"LLM:AnalysisMappings:{analysisType}"];
        if (!string.IsNullOrEmpty(providerName))
        {
            var provider = GetProvider(providerName);
            if (provider != null) return provider;
        }

        // Fallback to default
        return GetDefaultProvider();
    }

    /// <summary>
    /// Get all registered providers.
    /// </summary>
    public List<ILLMProvider> GetAllProviders()
    {
        return _providers.Values.ToList();
    }
}
