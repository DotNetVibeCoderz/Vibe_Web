using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace StockAnalyzer.Services.LLM;

/// <summary>
/// Google Gemini provider implementation.
/// </summary>
public class GeminiProvider : BaseLLMProvider
{
    public override string ProviderName => "Gemini";
    protected override string ApiEndpoint => "https://generativelanguage.googleapis.com/v1beta";

    public GeminiProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiProvider> logger)
        : base(httpClientFactory, configuration, logger)
    {
    }

    public override async Task<LLMResponse> SendMessageAsync(LLMRequest request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            if (!IsEnabled())
                return ErrorResponse("Gemini provider is disabled");

            var apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey) || apiKey == "your-gemini-api-key")
                return ErrorResponse("Gemini API key not configured");

            var client = CreateHttpClient();
            var baseUrl = GetBaseUrl();
            var model = request.ModelName ?? GetModelName() ?? "gemini-2.0-flash";

            // Gemini uses a different API format
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"{request.SystemPrompt}\n\n{request.UserPrompt}" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = request.Temperature,
                    maxOutputTokens = request.MaxTokens
                }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var url = $"{baseUrl}/models/{model}:generateContent?key={apiKey}";
            var response = await client.PostAsync(url, httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return ErrorResponse($"Gemini API error ({response.StatusCode}): {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var content = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            var tokensUsed = doc.RootElement.TryGetProperty("usageMetadata", out var usage)
                ? usage.GetProperty("totalTokenCount").GetInt32()
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
            return ErrorResponse($"Gemini exception: {ex.Message}");
        }
    }

    public override Task<bool> IsAvailableAsync()
    {
        if (!IsEnabled()) return Task.FromResult(false);
        var apiKey = GetApiKey();
        return Task.FromResult(!string.IsNullOrEmpty(apiKey) && apiKey != "your-gemini-api-key");
    }
}
