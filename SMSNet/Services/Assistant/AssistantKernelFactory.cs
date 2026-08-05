using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SMSNet.Services.Assistant.Plugins;

namespace SMSNet.Services.Assistant;

/// <summary>
/// Assembles a Semantic Kernel for the configured provider, with the school
/// plugins attached.
/// <para>
/// A kernel is built per conversation turn rather than cached, because the
/// plugin set is bound to the calling user's roles — reusing one kernel across
/// users would leak an admin's data access into a student's session.
/// </para>
/// </summary>
public sealed class AssistantKernelFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptionsMonitor<AssistantOptions> _options;

    public AssistantKernelFactory(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpFactory,
        ILoggerFactory loggerFactory,
        IOptionsMonitor<AssistantOptions> options)
    {
        _scopeFactory = scopeFactory;
        _httpFactory = httpFactory;
        _loggerFactory = loggerFactory;
        _options = options;
    }

    public AssistantOptions Options => _options.CurrentValue;

    public (Kernel Kernel, PromptExecutionSettings Settings) Build(AssistantUserContext user)
    {
        var options = _options.CurrentValue;
        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton(_loggerFactory);

        var settings = AttachProvider(builder, options);

        var kernel = builder.Build();

        kernel.Plugins.AddFromObject(new WaktuPlugin(), "Waktu");
        kernel.Plugins.AddFromObject(new MatematikaPlugin(), "Matematika");
        kernel.Plugins.AddFromObject(
            new WebPlugin(_httpFactory, _options.CurrentValue.ToOptionsWrapper(),
                _loggerFactory.CreateLogger<WebPlugin>()),
            "Web");
        kernel.Plugins.AddFromObject(new SekolahDataPlugin(_scopeFactory, user), "SekolahData");

        return (kernel, settings);
    }

    private PromptExecutionSettings AttachProvider(IKernelBuilder builder, AssistantOptions options)
    {
        // Auto invocation lets the model chain calls (search -> read page -> answer)
        // without the UI orchestrating each hop.
        var choice = options.EnableFunctionCalling
            ? FunctionChoiceBehavior.Auto()
            : FunctionChoiceBehavior.None();

        switch (options.Provider.Trim().ToLowerInvariant())
        {
            case "azureopenai":
                // Azure routes by deployment name and requires an api-version, so it needs
                // its own connector rather than an endpoint override on the OpenAI one.
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: options.AzureOpenAI.Deployment,
                    endpoint: options.AzureOpenAI.Endpoint,
                    apiKey: options.AzureOpenAI.ApiKey,
                    modelId: string.IsNullOrWhiteSpace(options.AzureOpenAI.ModelId)
                        ? null
                        : options.AzureOpenAI.ModelId);

                // On Azure the deployment name is what the operator sees, and it is often
                // the only clue to the underlying model — so check both.
                var azureReasoning = IsReasoningModel(options.AzureOpenAI.ModelId)
                                     || IsReasoningModel(options.AzureOpenAI.Deployment);

                return new OpenAIPromptExecutionSettings
                {
                    Temperature = azureReasoning ? null : options.Temperature,
                    TopP = azureReasoning ? null : options.TopP,
                    MaxTokens = azureReasoning ? null : options.MaxTokens,
                    FunctionChoiceBehavior = choice
                };

            case "anthropic":
                builder.Services.AddSingleton<IChatCompletionService>(
                    _ => new AnthropicChatCompletionService(
                        options,
                        _loggerFactory.CreateLogger<AnthropicChatCompletionService>()));

                // The connector reads MaxToolIterations from options and drives its
                // own tool loop, so there is nothing provider-specific to set here.
                return new PromptExecutionSettings();

            case "google":
                builder.AddGoogleAIGeminiChatCompletion(
                    modelId: options.Google.Model,
                    apiKey: options.Google.ApiKey);

                return new GeminiPromptExecutionSettings
                {
                    Temperature = options.Temperature,
                    TopP = options.TopP,
                    MaxTokens = options.MaxTokens,
                    FunctionChoiceBehavior = choice
                };

            case "ollama":
                builder.AddOllamaChatCompletion(
                    modelId: options.Ollama.Model,
                    endpoint: new Uri(options.Ollama.Endpoint));

                return new OllamaPromptExecutionSettings
                {
                    Temperature = (float)options.Temperature,
                    TopP = (float)options.TopP,
                    FunctionChoiceBehavior = choice
                };

            default:
                if (!string.IsNullOrWhiteSpace(options.OpenAI.Endpoint))
                {
                    var http = _httpFactory.CreateClient("assistant");
                    http.BaseAddress = new Uri(options.OpenAI.Endpoint);

                    builder.AddOpenAIChatCompletion(
                        modelId: options.OpenAI.Model,
                        apiKey: options.OpenAI.ApiKey,
                        orgId: string.IsNullOrWhiteSpace(options.OpenAI.OrganizationId) ? null : options.OpenAI.OrganizationId,
                        httpClient: http);
                }
                else
                {
                    builder.AddOpenAIChatCompletion(
                        modelId: options.OpenAI.Model,
                        apiKey: options.OpenAI.ApiKey,
                        orgId: string.IsNullOrWhiteSpace(options.OpenAI.OrganizationId) ? null : options.OpenAI.OrganizationId);
                }

                var openAiReasoning = IsReasoningModel(options.OpenAI.Model);

                return new OpenAIPromptExecutionSettings
                {
                    Temperature = openAiReasoning ? null : options.Temperature,
                    TopP = openAiReasoning ? null : options.TopP,
                    MaxTokens = openAiReasoning ? null : options.MaxTokens,
                    FunctionChoiceBehavior = choice
                };
        }
    }

    /// <summary>
    /// Whether a model is one of the reasoning families (gpt-5, o1/o3/o4) that reject
    /// the classic sampling parameters.
    /// <para>
    /// Those models require <c>max_completion_tokens</c> instead of <c>max_tokens</c>
    /// and allow only the default temperature and top_p. Semantic Kernel serialises the
    /// classic names, so sending any of them is an HTTP 400 — the fix is to leave all
    /// three unset and accept the service defaults.
    /// </para>
    /// </summary>
    private static bool IsReasoningModel(string? modelId)
    {
        var id = modelId?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        return id.StartsWith("gpt-5", StringComparison.Ordinal)
               || id.StartsWith("o1", StringComparison.Ordinal)
               || id.StartsWith("o3", StringComparison.Ordinal)
               || id.StartsWith("o4", StringComparison.Ordinal);
    }
}

internal static class OptionsWrapperExtensions
{
    /// <summary>Plugins take IOptions; the factory already holds the resolved value.</summary>
    public static IOptions<AssistantOptions> ToOptionsWrapper(this AssistantOptions options) =>
        Microsoft.Extensions.Options.Options.Create(options);
}
