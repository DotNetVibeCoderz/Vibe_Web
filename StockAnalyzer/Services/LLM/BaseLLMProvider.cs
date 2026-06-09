using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace StockAnalyzer.Services.LLM;

/// <summary>
/// Base class for LLM providers with common HTTP functionality.
/// </summary>
public abstract class BaseLLMProvider : ILLMProvider
{
    protected readonly IHttpClientFactory _httpClientFactory;
    protected readonly IConfiguration _configuration;
    protected readonly ILogger _logger;
    protected readonly JsonSerializerOptions _jsonOptions;

    public abstract string ProviderName { get; }
    protected abstract string ApiEndpoint { get; }

    protected BaseLLMProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public abstract Task<LLMResponse> SendMessageAsync(LLMRequest request);

    public abstract Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get API key from configuration.
    /// </summary>
    protected string? GetApiKey()
    {
        return _configuration[$"LLM:Providers:{ProviderName}:ApiKey"];
    }

    /// <summary>
    /// Get model name from configuration.
    /// </summary>
    protected string? GetModelName()
    {
        return _configuration[$"LLM:Providers:{ProviderName}:ModelName"];
    }

    /// <summary>
    /// Get base URL from configuration, with fallback.
    /// </summary>
    protected string GetBaseUrl()
    {
        return _configuration[$"LLM:Providers:{ProviderName}:ApiBaseUrl"] ?? ApiEndpoint;
    }

    /// <summary>
    /// Check if this provider is enabled in configuration.
    /// </summary>
    protected bool IsEnabled()
    {
        return _configuration.GetValue<bool>($"LLM:Providers:{ProviderName}:IsEnabled");
    }

    /// <summary>
    /// Create a standard HTTP client for LLM API calls.
    /// </summary>
    protected HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient("LLMClient");
        var timeout = _configuration.GetValue<int>($"LLM:Providers:{ProviderName}:TimeoutSeconds");
        if (timeout > 0)
            client.Timeout = TimeSpan.FromSeconds(timeout);
        return client;
    }

    /// <summary>
    /// Log and create an error response.
    /// </summary>
    protected LLMResponse ErrorResponse(string message)
    {
        _logger.LogError("{Provider}: {Message}", ProviderName, message);
        return new LLMResponse
        {
            ProviderName = ProviderName,
            ModelName = GetModelName() ?? "unknown",
            IsSuccess = false,
            ErrorMessage = message
        };
    }
}
