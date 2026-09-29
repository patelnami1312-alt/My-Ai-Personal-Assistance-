using System.Text.Json;
using System.Text.Json.Nodes;

namespace HomeAgent.Core.Llm;

public enum LlmRole { System, User, Assistant, Tool }

/// <summary>Provider-agnostic chat message passed to an <see cref="ILlmProvider"/>.</summary>
public record LlmMessage
{
    public LlmRole Role { get; init; }
    public string? Text { get; init; }
    public IReadOnlyList<LlmToolCall> ToolCalls { get; init; } = Array.Empty<LlmToolCall>();
    public IReadOnlyList<LlmToolResult> ToolResults { get; init; } = Array.Empty<LlmToolResult>();

    public static LlmMessage User(string text) => new() { Role = LlmRole.User, Text = text };
    public static LlmMessage Assistant(string text) => new() { Role = LlmRole.Assistant, Text = text };
}

public record LlmToolCall(string Id, string Name, string ArgumentsJson)
{
    public JsonObject Arguments()
    {
        try { return JsonNode.Parse(string.IsNullOrWhiteSpace(ArgumentsJson) ? "{}" : ArgumentsJson)?.AsObject() ?? new(); }
        catch { return new(); }
    }
}

public record LlmToolResult(string ToolCallId, string Content, bool IsError = false);

public record LlmToolDefinition(string Name, string Description, JsonObject ParametersSchema);

public record LlmResponse
{
    public string? Text { get; init; }
    public IReadOnlyList<LlmToolCall> ToolCalls { get; init; } = Array.Empty<LlmToolCall>();
    public string? StopReason { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public bool WantsToolUse => ToolCalls.Count > 0;
}

public record LlmRequest
{
    public required IReadOnlyList<LlmMessage> Messages { get; init; }
    public string? SystemPrompt { get; init; }
    public IReadOnlyList<LlmToolDefinition> Tools { get; init; } = Array.Empty<LlmToolDefinition>();
    public double Temperature { get; init; } = 0.4;
    public int MaxTokens { get; init; } = 1500;
}
