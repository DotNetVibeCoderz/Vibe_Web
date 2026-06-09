using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace StockAnalyzer.Services.LLM;

/// <summary>
/// OpenAI provider implementation.
/// Compatible with OpenAI API and any OpenAI-compatible API (LM Studio, vLLM, etc.).
/// </summary>
public class OpenAIProvider : BaseLLMProvider
{
    public override string ProviderName => "OpenAI";
    protected override string ApiEndpoint => "https://api.openai.com/v1";

    public OpenAIProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenAIProvider> logger)
        : base(httpClientFactory, configuration, logger)
    {
    }

    public override async Task<LLMResponse> SendMessageAsync(LLMRequest request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            if (!IsEnabled())
                return ErrorResponse("OpenAI provider is disabled");

            var apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("sk-your-"))
                return ErrorResponse("OpenAI API key not configured");

            var client = CreateHttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var baseUrl = GetBaseUrl();
            var model = request.ModelName ?? GetModelName() ?? "gpt-4o";

            var payload = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                temperature = request.Temperature,
                max_tokens = request.MaxTokens
            };

            var jsonContent = JsonSerializer.Serialize(payload, _jsonOptions);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{baseUrl}/chat/completions", httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return ErrorResponse($"OpenAI API error ({response.StatusCode}): {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            var tokensUsed = doc.RootElement.TryGetProperty("usage", out var usage)
                ? usage.GetProperty("total_tokens").GetInt32()
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
            return ErrorResponse($"OpenAI exception: {ex.Message}");
        }
    }

    public override async Task<bool> IsAvailableAsync()
    {
        if (!IsEnabled()) return false;
        var apiKey = GetApiKey();
        return !string.IsNullOrEmpty(apiKey) && !apiKey.StartsWith("sk-your-");
    }
}
