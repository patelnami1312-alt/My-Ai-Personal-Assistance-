using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Assistant.Core.Abstractions;
using Assistant.Core.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Assistant.Infrastructure.Llm;

public class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>"anthropic" (default) or "openai". Selects which <see cref="ILlmProvider"/> is wired up.</summary>
    public string Provider { get; set; } = "anthropic";

    /// <summary>API key for the selected provider. Falls back to ANTHROPIC_API_KEY / OPENAI_API_KEY.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Model id for the selected provider.
    /// anthropic: e.g. "claude-opus-5" (default) or the cheaper "claude-sonnet-5".
    /// openai: e.g. "gpt-4o" (default) or "gpt-4o-mini".
    /// </summary>
    public string Model { get; set; } = "claude-opus-5";

    public int MaxTokens { get; set; } = 1500;
}

/// <summary>Claude implementation of <see cref="ILlmProvider"/> using the official Anthropic SDK.</summary>
public class AnthropicLlmProvider : ILlmProvider
{
    private readonly AnthropicClient _client;
    private readonly LlmOptions _opts;
    private readonly ILogger<AnthropicLlmProvider> _log;

    public AnthropicLlmProvider(IOptions<LlmOptions> opts, ILogger<AnthropicLlmProvider> log)
    {
        _opts = opts.Value;
        _log = log;
        var apiKey = _opts.ApiKey
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException(
                "No Anthropic API key. Set Llm:ApiKey in configuration/user-secrets or the ANTHROPIC_API_KEY environment variable.");
        _client = new AnthropicClient { ApiKey = apiKey };
    }

    public string Name => $"anthropic:{_opts.Model}";

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        var messages = request.Messages
            .Where(m => m.Role != LlmRole.System)
            .Select(ToMessageParam)
            .ToList();

        var parameters = new MessageCreateParams
        {
            Model = _opts.Model,
            MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : _opts.MaxTokens,
            Messages = messages,
            System = string.IsNullOrWhiteSpace(request.SystemPrompt) ? null : request.SystemPrompt,
            Tools = request.Tools.Count > 0 ? request.Tools.Select(ToToolUnion).ToList() : null,
        };

        Message response;
        try
        {
            response = await _client.Messages.Create(parameters, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Anthropic Messages.Create failed");
            throw;
        }

        var textParts = new List<string>();
        var toolCalls = new List<LlmToolCall>();
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out TextBlock? text) && text is not null)
            {
                textParts.Add(text.Text);
            }
            else if (block.TryPickToolUse(out ToolUseBlock? tu) && tu is not null)
            {
                toolCalls.Add(new LlmToolCall(tu.ID, tu.Name, JsonSerializer.Serialize(tu.Input)));
            }
        }

        return new LlmResponse
        {
            Text = textParts.Count > 0 ? string.Join("\n", textParts) : null,
            ToolCalls = toolCalls,
            StopReason = response.StopReason?.ToString(),
            InputTokens = (int)(response.Usage?.InputTokens ?? 0),
            OutputTokens = (int)(response.Usage?.OutputTokens ?? 0),
        };
    }

    private static MessageParam ToMessageParam(LlmMessage m)
    {
        switch (m.Role)
        {
            case LlmRole.User:
                return new MessageParam { Role = Role.User, Content = m.Text ?? "" };

            case LlmRole.Assistant:
            {
                var blocks = new List<ContentBlockParam>();
                if (!string.IsNullOrWhiteSpace(m.Text))
                    blocks.Add(new TextBlockParam { Text = m.Text });
                foreach (var call in m.ToolCalls)
                {
                    blocks.Add(new ToolUseBlockParam
                    {
                        ID = call.Id,
                        Name = call.Name,
                        Input = ParseInput(call.ArgumentsJson),
                    });
                }
                if (blocks.Count == 0)
                    blocks.Add(new TextBlockParam { Text = "" });
                return new MessageParam { Role = Role.Assistant, Content = blocks };
            }

            case LlmRole.Tool:
            {
                var results = m.ToolResults
                    .Select(r => (ContentBlockParam)new ToolResultBlockParam
                    {
                        ToolUseID = r.ToolCallId,
                        Content = r.Content,
                        IsError = r.IsError,
                    })
                    .ToList();
                return new MessageParam { Role = Role.User, Content = results };
            }

            default:
                return new MessageParam { Role = Role.User, Content = m.Text ?? "" };
        }
    }

    private static Dictionary<string, JsonElement> ParseInput(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static ToolUnion ToToolUnion(LlmToolDefinition def)
    {
        var props = new Dictionary<string, JsonElement>();
        var required = new List<string>();

        if (def.ParametersSchema.TryGetPropertyValue("properties", out var propsNode) && propsNode is not null)
        {
            foreach (var kv in propsNode.AsObject())
            {
                props[kv.Key] = JsonSerializer.SerializeToElement(kv.Value);
            }
        }
        if (def.ParametersSchema.TryGetPropertyValue("required", out var reqNode) && reqNode is not null)
        {
            foreach (var item in reqNode.AsArray())
                if (item is not null) required.Add(item.GetValue<string>());
        }

        return new Tool
        {
            Name = def.Name,
            Description = def.Description,
            InputSchema = new()
            {
                Properties = props,
                Required = required,
            },
        };
    }
}
