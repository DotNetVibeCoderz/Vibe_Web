using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SMSNet.Services.Assistant;

/// <summary>
/// Semantic Kernel chat connector for Claude, built on the official Anthropic SDK.
/// <para>
/// Microsoft does not ship an Anthropic connector, and the community package is
/// several minor versions behind the Semantic Kernel we target — so rather than
/// pin a mismatched dependency this implements <see cref="IChatCompletionService"/>
/// directly. Auto function calling lives here too: every SK connector owns its
/// own tool loop, so there is nothing to inherit.
/// </para>
/// </summary>
public sealed class AnthropicChatCompletionService : IChatCompletionService
{
    /// <summary>SK addresses functions as plugin+function; Anthropic tool names allow [a-zA-Z0-9_-].</summary>
    private const string NameSeparator = "__";

    private readonly AnthropicClient _client;
    private readonly AssistantOptions _options;
    private readonly ILogger<AnthropicChatCompletionService> _logger;

    public AnthropicChatCompletionService(
        AssistantOptions options,
        ILogger<AnthropicChatCompletionService> logger)
    {
        _options = options;
        _logger = logger;
        _client = new AnthropicClient { ApiKey = options.Anthropic.ApiKey };
    }

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>
    {
        ["ModelId"] = "anthropic"
    };

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var system = ExtractSystemPrompt(chatHistory);
        var messages = BuildMessages(chatHistory);
        var tools = BuildTools(kernel);

        var usedTools = new List<string>();
        var answer = new StringBuilder();

        for (var iteration = 0; iteration < Math.Max(1, _options.MaxToolIterations); iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _client.Messages.Create(
                BuildRequest(system, messages, tools),
                cancellationToken);

            // A safety classifier declined the request. This arrives as a normal
            // 200 with an empty content list, so it must be checked before reading.
            if (response.StopReason == "refusal")
            {
                var explanation = response.StopDetails?.Explanation;
                return Single(string.IsNullOrWhiteSpace(explanation)
                    ? "Maaf, permintaan ini tidak dapat saya proses karena kebijakan keamanan."
                    : $"Maaf, permintaan ini tidak dapat saya proses: {explanation}");
            }

            var assistantBlocks = new List<ContentBlockParam>();
            var toolCalls = new List<ToolUseBlock>();

            foreach (var block in response.Content)
            {
                if (block.TryPickText(out var text))
                {
                    answer.Append(text!.Text);
                    assistantBlocks.Add(new TextBlockParam { Text = text.Text });
                }
                else if (block.TryPickThinking(out var thinking))
                {
                    // Echoed back unchanged — the API rejects edited thinking blocks.
                    assistantBlocks.Add(new ThinkingBlockParam
                    {
                        Thinking = thinking!.Thinking,
                        Signature = thinking.Signature
                    });
                }
                else if (block.TryPickToolUse(out var toolUse))
                {
                    toolCalls.Add(toolUse!);
                    assistantBlocks.Add(new ToolUseBlockParam
                    {
                        ID = toolUse.ID,
                        Name = toolUse.Name,
                        Input = toolUse.Input
                    });
                }
            }

            if (toolCalls.Count == 0 || kernel is null)
            {
                break;
            }

            messages.Add(new MessageParam { Role = Role.Assistant, Content = assistantBlocks });

            // Every tool_use must get a matching tool_result in one user message,
            // otherwise the follow-up request is rejected.
            var results = new List<ContentBlockParam>();
            foreach (var call in toolCalls)
            {
                usedTools.Add(call.Name);
                var (payload, failed) = await InvokeAsync(kernel, call, cancellationToken);
                results.Add(new ToolResultBlockParam
                {
                    ToolUseID = call.ID,
                    Content = payload,
                    IsError = failed
                });
            }

            messages.Add(new MessageParam { Role = Role.User, Content = results });
            answer.Clear();
        }

        var content = Single(answer.ToString());
        if (usedTools.Count > 0)
        {
            content[0].Metadata = new Dictionary<string, object?>
            {
                ["ToolsUsed"] = usedTools.Distinct().ToArray()
            };
        }

        return content;
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Tool calls have to complete before there is a final answer to stream, so
        // the loop above runs first and the result is handed back in one chunk.
        var result = await GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);

        foreach (var message in result)
        {
            yield return new StreamingChatMessageContent(message.Role, message.Content)
            {
                Metadata = message.Metadata
            };
        }
    }

    private MessageCreateParams BuildRequest(string? system, List<MessageParam> messages, List<ToolUnion>? tools)
    {
        // These properties are init-only, so everything is decided in one initializer.
        return new MessageCreateParams
        {
            Model = _options.Anthropic.Model,
            MaxTokens = _options.MaxTokens,
            Messages = messages,
            // Note: temperature/top_p are deliberately absent. Current Claude models
            // reject them with a 400 — depth is controlled by effort instead.
            OutputConfig = new OutputConfig { Effort = ParseEffort(_options.Anthropic.Effort) },
            System = string.IsNullOrWhiteSpace(system)
                ? null
                : new List<TextBlockParam> { new() { Text = system } },
            Tools = tools is { Count: > 0 } ? tools : null,
            Thinking = _options.Anthropic.ShowThinking
                ? new ThinkingConfigAdaptive { Display = Display.Summarized }
                : null
        };
    }

    private static Effort ParseEffort(string value) => value?.Trim().ToLowerInvariant() switch
    {
        "low" => Effort.Low,
        "high" => Effort.High,
        "max" => Effort.Max,
        _ => Effort.Medium
    };

    private static List<ChatMessageContent> Single(string text) => new()
    {
        new ChatMessageContent(AuthorRole.Assistant, text)
    };

    private static string? ExtractSystemPrompt(ChatHistory history)
    {
        var parts = history
            .Where(m => m.Role == AuthorRole.System)
            .Select(m => m.Content)
            .Where(c => !string.IsNullOrWhiteSpace(c));

        var joined = string.Join("\n\n", parts);
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }

    private static List<MessageParam> BuildMessages(ChatHistory history)
    {
        // Turns are accumulated as plain lists and only turned into MessageParam at
        // the end — MessageParam.Content is a union that is far easier to write than
        // to read back when consecutive same-role turns need merging.
        var turns = new List<(Role Role, List<ContentBlockParam> Blocks)>();

        foreach (var message in history.Where(m => m.Role != AuthorRole.System))
        {
            var role = message.Role == AuthorRole.Assistant ? Role.Assistant : Role.User;
            var blocks = new List<ContentBlockParam>();

            foreach (var item in message.Items)
            {
                switch (item)
                {
                    case TextContent { Text: { Length: > 0 } text }:
                        blocks.Add(new TextBlockParam { Text = text });
                        break;

                    case ImageContent image:
                        var block = ToImageBlock(image);
                        if (block is not null)
                        {
                            blocks.Add(block);
                        }
                        break;
                }
            }

            if (blocks.Count == 0 && !string.IsNullOrWhiteSpace(message.Content))
            {
                blocks.Add(new TextBlockParam { Text = message.Content });
            }

            if (blocks.Count == 0)
            {
                continue;
            }

            // The API rejects two consecutive turns with the same role, which is
            // easy to produce once attachments are split across items.
            if (turns.Count > 0 && turns[^1].Role == role)
            {
                turns[^1].Blocks.AddRange(blocks);
                continue;
            }

            turns.Add((role, blocks));
        }

        // A conversation must open on the user turn.
        while (turns.Count > 0 && turns[0].Role == Role.Assistant)
        {
            turns.RemoveAt(0);
        }

        return turns
            .Select(t => new MessageParam { Role = t.Role, Content = t.Blocks })
            .ToList();
    }

    private static ImageBlockParam? ToImageBlock(ImageContent image)
    {
        if (image.Data is { Length: > 0 } data)
        {
            return new ImageBlockParam
            {
                Source = new Base64ImageSource
                {
                    MediaType = MapMediaType(image.MimeType),
                    Data = Convert.ToBase64String(data.ToArray())
                }
            };
        }

        return image.Uri is not null
            ? new ImageBlockParam { Source = new UrlImageSource { Url = image.Uri.ToString() } }
            : null;
    }

    /// <summary>
    /// The SDK's media-type enum covers PNG, JPEG, and GIF only, so WebP uploads are
    /// filtered out before they reach here (see <see cref="AssistantOptions.UploadOptions"/>).
    /// </summary>
    private static MediaType MapMediaType(string? mime) => mime?.ToLowerInvariant() switch
    {
        "image/png" => MediaType.ImagePng,
        "image/gif" => MediaType.ImageGif,
        _ => MediaType.ImageJpeg
    };

    private List<ToolUnion>? BuildTools(Kernel? kernel)
    {
        if (kernel is null || !_options.EnableFunctionCalling)
        {
            return null;
        }

        var tools = new List<ToolUnion>();

        foreach (var plugin in kernel.Plugins)
        {
            foreach (var function in plugin)
            {
                var properties = new Dictionary<string, JsonElement>();
                var required = new List<string>();

                foreach (var parameter in function.Metadata.Parameters)
                {
                    if (parameter.Schema is not null)
                    {
                        properties[parameter.Name] = parameter.Schema.RootElement;
                    }
                    else
                    {
                        properties[parameter.Name] = JsonSerializer.SerializeToElement(new
                        {
                            type = "string",
                            description = parameter.Description ?? parameter.Name
                        });
                    }

                    if (parameter.IsRequired)
                    {
                        required.Add(parameter.Name);
                    }
                }

                tools.Add(new Tool
                {
                    Name = $"{plugin.Name}{NameSeparator}{function.Name}",
                    Description = function.Description,
                    InputSchema = new InputSchema
                    {
                        Properties = properties,
                        Required = required
                    }
                });
            }
        }

        return tools.Count == 0 ? null : tools;
    }

    private async Task<(string Payload, bool Failed)> InvokeAsync(
        Kernel kernel,
        ToolUseBlock call,
        CancellationToken cancellationToken)
    {
        var separatorIndex = call.Name.IndexOf(NameSeparator, StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return ($"Nama fungsi tidak dikenal: {call.Name}", true);
        }

        var pluginName = call.Name[..separatorIndex];
        var functionName = call.Name[(separatorIndex + NameSeparator.Length)..];

        if (!kernel.Plugins.TryGetFunction(pluginName, functionName, out var function))
        {
            return ($"Fungsi {call.Name} tidak tersedia.", true);
        }

        var arguments = new KernelArguments();
        foreach (var (key, value) in call.Input)
        {
            arguments[key] = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.TryGetInt64(out var i) ? i : value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => value.GetRawText()
            };
        }

        try
        {
            var result = await function.InvokeAsync(kernel, arguments, cancellationToken);
            return (result.ToString() ?? string.Empty, false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Kernel function {Function} failed", call.Name);
            return ($"Fungsi gagal dijalankan: {ex.Message}", true);
        }
    }
}
