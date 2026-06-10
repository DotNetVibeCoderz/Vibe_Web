using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lapak.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Lapak.Services.AI;

public class LlmMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<LlmMessage> Messages { get; set; } = new();

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 2000;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}

public class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<ChatChoice> Choices { get; set; } = new();
}

public class ChatChoice
{
    [JsonPropertyName("message")]
    public LlmMessage Message { get; set; } = new();
}

public interface ILlmService
{
    Task<string> ChatAsync(string systemPrompt, string userMessage, string? providerName = null, CancellationToken ct = default);
    IAsyncEnumerable<string> ChatStreamAsync(string systemPrompt, string userMessage, string? providerName = null, CancellationToken ct = default);
    Task<string> ChatWithHistoryAsync(string systemPrompt, List<LlmMessage> history, string? providerName = null, CancellationToken ct = default);
}

public class LlmService : ILlmService
{
    private readonly AiConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LlmService> _logger;

    private static readonly string[] FallbackOrder = { "OpenAI", "Gemini", "Anthropic", "Ollama" };

    public LlmService(
        IOptions<AiConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<LlmService> logger)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> ChatAsync(string systemPrompt, string userMessage, string? providerName = null, CancellationToken ct = default)
    {
        var providers = GetProviderPriorityList(providerName);

        Exception? lastException = null;
        foreach (var provider in providers)
        {
            try
            {
                _logger.LogInformation("Trying LLM provider: {Provider}", provider);
                var result = await SendToProviderAsync(provider, systemPrompt, userMessage, ct);
                _logger.LogInformation("Provider {Provider} succeeded", provider);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed: {Message}", provider, ex.Message);
                lastException = ex;
                if (!_config.FallbackEnabled) break;
            }
        }

        throw new InvalidOperationException(
            "All LLM providers failed. Last error: " + (lastException?.Message ?? "Unknown"),
            lastException);
    }

    /// <summary>
    /// Streaming chat - collects all tokens first to avoid yield in try-catch
    /// </summary>
    public async IAsyncEnumerable<string> ChatStreamAsync(string systemPrompt, string userMessage, string? providerName = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var providers = GetProviderPriorityList(providerName);
        List<string>? collectedTokens = null;

        foreach (var provider in providers)
        {
            collectedTokens = await TryStreamFromProviderAsync(provider, systemPrompt, userMessage, ct);
            if (collectedTokens != null && collectedTokens.Count > 0)
            {
                foreach (var token in collectedTokens)
                {
                    yield return token;
                }
                yield break;
            }
            if (!_config.FallbackEnabled) break;
        }

        yield return "[Error: All providers failed]";
    }

    /// <summary>
    /// Helper: tries to stream from a provider, returns collected tokens or null
    /// </summary>
    private async Task<List<string>?> TryStreamFromProviderAsync(string provider, string systemPrompt, string userMessage, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Streaming from provider: {Provider}", provider);

            var config = _config.Providers.GetValueOrDefault(provider);
            if (config == null || string.IsNullOrEmpty(config.ApiKey))
                return null;

            var request = new ChatCompletionRequest
            {
                Model = config.Model,
                Messages = new List<LlmMessage>
                {
                    new() { Role = "system", Content = systemPrompt },
                    new() { Role = "user", Content = userMessage }
                },
                MaxTokens = config.MaxTokens,
                Temperature = config.Temperature,
                Stream = true
            };

            var client = _httpClientFactory.CreateClient("LlmClient");
            client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            SetAuthHeader(client, provider, config);
            var endpoint = GetChatEndpoint(provider, config);

            var response = await client.PostAsync(endpoint, content, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            var tokens = new List<string>();
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null && !ct.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;

                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var delta = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("delta");

                    if (delta.TryGetProperty("content", out var contentElement))
                    {
                        var token = contentElement.GetString();
                        if (!string.IsNullOrEmpty(token))
                            tokens.Add(token);
                    }
                }
                catch { /* Skip malformed SSE chunks */ }
            }

            return tokens;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Streaming provider {Provider} failed", provider);
            return null;
        }
    }

    public async Task<string> ChatWithHistoryAsync(string systemPrompt, List<LlmMessage> history, string? providerName = null, CancellationToken ct = default)
    {
        var providers = GetProviderPriorityList(providerName);

        Exception? lastException = null;
        foreach (var provider in providers)
        {
            try
            {
                var config = _config.Providers.GetValueOrDefault(provider);
                if (config == null || string.IsNullOrEmpty(config.ApiKey)) continue;

                var messages = new List<LlmMessage>
                {
                    new() { Role = "system", Content = systemPrompt }
                };
                messages.AddRange(history);

                var request = new ChatCompletionRequest
                {
                    Model = config.Model,
                    Messages = messages,
                    MaxTokens = config.MaxTokens,
                    Temperature = config.Temperature
                };

                var client = _httpClientFactory.CreateClient("LlmClient");
                client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                SetAuthHeader(client, provider, config);
                var endpoint = GetChatEndpoint(provider, config);

                var response = await client.PostAsync(endpoint, content, ct);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);

                return result?.Choices?.FirstOrDefault()?.Message?.Content
                    ?? "Maaf, tidak ada respon dari AI.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed", provider);
                lastException = ex;
                if (!_config.FallbackEnabled) break;
            }
        }

        throw new InvalidOperationException("All providers failed: " + (lastException?.Message ?? "Unknown"));
    }

    private async Task<string> SendToProviderAsync(string provider, string systemPrompt, string userMessage, CancellationToken ct)
    {
        var config = _config.Providers.GetValueOrDefault(provider)
            ?? throw new InvalidOperationException("Provider '" + provider + "' not configured");

        if (string.IsNullOrEmpty(config.ApiKey))
            throw new InvalidOperationException("API key for '" + provider + "' is not set");

        var request = new ChatCompletionRequest
        {
            Model = config.Model,
            Messages = new List<LlmMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userMessage }
            },
            MaxTokens = config.MaxTokens,
            Temperature = config.Temperature
        };

        var client = _httpClientFactory.CreateClient("LlmClient");
        client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        SetAuthHeader(client, provider, config);
        var endpoint = GetChatEndpoint(provider, config);

        var response = await client.PostAsync(endpoint, content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);

        return result?.Choices?.FirstOrDefault()?.Message?.Content
            ?? "Maaf, tidak ada respon dari AI.";
    }

    private List<string> GetProviderPriorityList(string? preferredProvider)
    {
        var providers = new List<string>();

        if (!string.IsNullOrEmpty(preferredProvider))
            providers.Add(preferredProvider);

        foreach (var p in FallbackOrder)
        {
            if (!providers.Contains(p))
                providers.Add(p);
        }

        return providers;
    }

    private void SetAuthHeader(HttpClient client, string provider, AiProviderConfig config)
    {
        switch (provider)
        {
            case "Anthropic":
                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                break;
            case "Gemini":
                break;
            default:
                client.DefaultRequestHeaders.Remove("x-api-key");
                client.DefaultRequestHeaders.Remove("anthropic-version");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
                break;
        }
    }

    private string GetChatEndpoint(string provider, AiProviderConfig config)
    {
        return provider switch
        {
            "Gemini" => config.BaseUrl + "/models/" + config.Model + ":generateContent?key=" + config.ApiKey,
            "Anthropic" => config.BaseUrl + "/v1/messages",
            _ => config.BaseUrl + "/chat/completions"
        };
    }
}
