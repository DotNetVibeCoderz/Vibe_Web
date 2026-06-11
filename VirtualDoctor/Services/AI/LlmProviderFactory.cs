using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services.AI;

public class LlmProviderFactory : ILlmProviderFactory
{
    private readonly AppConfig _cfg;
    private readonly Dictionary<string, Kernel> _cache = new();

    public LlmProviderFactory(AppConfig cfg) => _cfg = cfg;
    public string SystemPrompt => _cfg.Llm.SystemPrompt;

    public List<string> GetAvailableProviders()
    {
        var p = new List<string>();
        if (HasOpenAI()) p.Add("OpenAI");
        if (HasGemini()) p.Add("Gemini");
        if (HasAnthropic()) p.Add("Anthropic");
        if (HasOllama()) p.Add("Ollama");
        if (HasOpenAICompat()) p.Add("OpenAICompatible");
        if (p.Count == 0) p.Add("OpenAI");
        return p;
    }

    public Kernel GetKernel(string? provider = null, double? temperature = null)
    {
        provider ??= _cfg.Llm.DefaultProvider;
        if (_cache.TryGetValue(provider, out var cached)) return cached;

        var builder = Kernel.CreateBuilder();
        switch (provider)
        {
            case "OpenAI":
                var oc = _cfg.Llm.OpenAI!;
                builder.AddOpenAIChatCompletion(oc.Model, oc.ApiKey);
                break;
            case "Gemini":
                var gc = _cfg.Llm.Gemini!;
                builder.AddOpenAIChatCompletion(gc.Model, gc.ApiKey, "https://generativelanguage.googleapis.com/v1beta/openai/");
                break;
            case "Anthropic":
                var ac = _cfg.Llm.Anthropic!;
                builder.AddOpenAIChatCompletion(ac.Model, ac.ApiKey);
                break;
            case "Ollama":
                var ol = _cfg.Llm.Ollama!;
                builder.AddOpenAIChatCompletion(ol.Model, "ollama", ol.Endpoint.TrimEnd('/') + "/v1/");
                break;
            case "OpenAICompatible":
                var xc = _cfg.Llm.OpenAICompatible!;
                builder.AddOpenAIChatCompletion(xc.Model, xc.ApiKey ?? "-", xc.Endpoint.TrimEnd('/') + "/v1/");
                break;
            default:
                builder.AddOpenAIChatCompletion(_cfg.Llm.OpenAI!.Model, _cfg.Llm.OpenAI.ApiKey);
                break;
        }
        var k = builder.Build();
        _cache[provider] = k;
        return k;
    }

    public IChatCompletionService GetChatService(string? p = null) => GetKernel(p).GetRequiredService<IChatCompletionService>();

    public OpenAIPromptExecutionSettings GetExecutionSettings(string? p = null, double? temp = null, bool enableFuncs = false)
    {
        var s = new OpenAIPromptExecutionSettings { Temperature = temp ?? _cfg.Llm.Temperature, MaxTokens = _cfg.Llm.MaxTokens };
        if (enableFuncs) s.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
        return s;
    }

    public Kernel GetBaseKernel() => Kernel.CreateBuilder().Build();

    bool HasOpenAI() => !string.IsNullOrEmpty(_cfg.Llm.OpenAI?.ApiKey) && _cfg.Llm.OpenAI.ApiKey != "YOUR_OPENAI_API_KEY";
    bool HasGemini() => !string.IsNullOrEmpty(_cfg.Llm.Gemini?.ApiKey) && _cfg.Llm.Gemini.ApiKey != "YOUR_GEMINI_API_KEY";
    bool HasAnthropic() => !string.IsNullOrEmpty(_cfg.Llm.Anthropic?.ApiKey) && _cfg.Llm.Anthropic.ApiKey != "YOUR_ANTHROPIC_API_KEY";
    bool HasOllama() => !string.IsNullOrEmpty(_cfg.Llm.Ollama?.Endpoint);
    bool HasOpenAICompat() => !string.IsNullOrEmpty(_cfg.Llm.OpenAICompatible?.Endpoint);
}
