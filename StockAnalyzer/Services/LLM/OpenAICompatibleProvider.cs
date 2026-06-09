using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace StockAnalyzer.Services.LLM;

/// <summary>
/// OpenAI-compatible provider for custom endpoints (LM Studio, vLLM, Groq, etc.).
/// Uses OpenAI API format but points to a custom base URL.
/// </summary>
public class OpenAICompatibleProvider : BaseLLMProvider
{
    public override string ProviderName => "OpenAICompatible";
    protected override string ApiEndpoint => "http://localhost:1234/v1";

    public OpenAICompatibleProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenAICompatibleProvider> logger)
        : base(httpClientFactory, configuration, logger)
    {
    }

    public override async Task<LLMResponse> SendMessageAsync(LLMRequest request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            if (!IsEnabled())
                return ErrorResponse("OpenAI-compatible provider is disabled");

            var client = CreateHttpClient();
            var baseUrl = GetBaseUrl();
            var model = request.ModelName ?? GetModelName() ?? "local-model";

            // Standard OpenAI-compatible API
            var payload = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Add API key header if configured
            var apiKey = GetApiKey();
            if (!string.IsNullOrEmpty(apiKey) && apiKey != "not-needed")
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }

            var response = await client.PostAsync($"{baseUrl}/chat/completions", httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return ErrorResponse($"OpenAI-compatible API error ({response.StatusCode}): {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            return new LLMResponse
            {
                Content = content,
                ProviderName = ProviderName,
                ModelName = model,
                IsSuccess = true,
                ResponseTime = DateTime.UtcNow - startTime,
                TokensUsed = doc.RootElement.TryGetProperty("usage", out var usage)
                    ? usage.GetProperty("total_tokens").GetInt32() : 0
            };
        }
        catch (HttpRequestException)
        {
            return ErrorResponse("OpenAI-compatible endpoint is not reachable. Check the URL and ensure the server is running.");
        }
        catch (Exception ex)
        {
            return ErrorResponse($"OpenAI-compatible exception: {ex.Message}");
        }
    }

    public override async Task<bool> IsAvailableAsync()
    {
        if (!IsEnabled()) return false;

        try
        {
            var client = CreateHttpClient();
            var baseUrl = GetBaseUrl();
            var response = await client.GetAsync($"{baseUrl}/models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
