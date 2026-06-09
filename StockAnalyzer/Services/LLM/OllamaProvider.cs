using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace StockAnalyzer.Services.LLM;

/// <summary>
/// Ollama provider implementation for local LLM models.
/// Ollama uses an OpenAI-compatible API.
/// </summary>
public class OllamaProvider : BaseLLMProvider
{
    public override string ProviderName => "Ollama";
    protected override string ApiEndpoint => "http://localhost:11434";

    public OllamaProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OllamaProvider> logger)
        : base(httpClientFactory, configuration, logger)
    {
    }

    public override async Task<LLMResponse> SendMessageAsync(LLMRequest request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            if (!IsEnabled())
                return ErrorResponse("Ollama provider is disabled");

            var client = CreateHttpClient();
            var baseUrl = GetBaseUrl();
            var model = request.ModelName ?? GetModelName() ?? "llama3.1";

            // Ollama uses OpenAI-compatible API format
            var payload = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                stream = false,
                options = new
                {
                    temperature = request.Temperature,
                    num_predict = request.MaxTokens
                }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{baseUrl}/api/chat", httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return ErrorResponse($"Ollama API error ({response.StatusCode}): {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var content = doc.RootElement
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
                TokensUsed = doc.RootElement.TryGetProperty("eval_count", out var eval)
                    ? eval.GetInt32() : 0
            };
        }
        catch (HttpRequestException)
        {
            // Ollama is likely not running locally
            return ErrorResponse("Ollama is not running or not reachable. Make sure Ollama is installed and running.");
        }
        catch (Exception ex)
        {
            return ErrorResponse($"Ollama exception: {ex.Message}");
        }
    }

    public override async Task<bool> IsAvailableAsync()
    {
        if (!IsEnabled()) return false;

        try
        {
            var client = CreateHttpClient();
            var baseUrl = GetBaseUrl();
            var response = await client.GetAsync($"{baseUrl}/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
