using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace StockAnalyzer.Services.LLM;

/// <summary>
/// Anthropic Claude provider implementation.
/// </summary>
public class AnthropicProvider : BaseLLMProvider
{
    public override string ProviderName => "Anthropic";
    protected override string ApiEndpoint => "https://api.anthropic.com/v1";

    public AnthropicProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AnthropicProvider> logger)
        : base(httpClientFactory, configuration, logger)
    {
    }

    public override async Task<LLMResponse> SendMessageAsync(LLMRequest request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            if (!IsEnabled())
                return ErrorResponse("Anthropic provider is disabled");

            var apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey) || apiKey == "your-anthropic-api-key")
                return ErrorResponse("Anthropic API key not configured");

            var client = CreateHttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var baseUrl = GetBaseUrl();
            var model = request.ModelName ?? GetModelName() ?? "claude-3-sonnet-20240229";

            var payload = new
            {
                model,
                system = request.SystemPrompt,
                messages = new[]
                {
                    new { role = "user", content = request.UserPrompt }
                },
                temperature = request.Temperature,
                max_tokens = request.MaxTokens
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{baseUrl}/messages", httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return ErrorResponse($"Anthropic API error ({response.StatusCode}): {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var content = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            var tokensUsed = doc.RootElement.TryGetProperty("usage", out var usage)
                ? usage.GetProperty("input_tokens").GetInt32() +
                  usage.GetProperty("output_tokens").GetInt32()
                : 0;

            return new LLMResponse
            {
                Content = content,
                ProviderName = ProviderName,
                ModelName = model,
                IsSuccess = true,
                ResponseTime = DateTime.UtcNow - startTime,
                TokensUsed = tokensUsed
            };
        }
        catch (Exception ex)
        {
            return ErrorResponse($"Anthropic exception: {ex.Message}");
        }
    }

    public override Task<bool> IsAvailableAsync()
    {
        if (!IsEnabled()) return Task.FromResult(false);
        var apiKey = GetApiKey();
        return Task.FromResult(!string.IsNullOrEmpty(apiKey) && apiKey != "your-anthropic-api-key");
    }
}
