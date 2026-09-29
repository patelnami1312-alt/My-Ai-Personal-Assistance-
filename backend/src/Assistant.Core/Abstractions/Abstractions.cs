using System.Text.Json.Nodes;
using Assistant.Core.Llm;

namespace Assistant.Core.Abstractions;

/// <summary>
/// A swappable LLM backend. The orchestrator only knows this interface, so moving from
/// Claude to OpenAI (or adding a local model) is a single new implementation + DI line.
/// </summary>
public interface ILlmProvider
{
    string Name { get; }
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default);
}

/// <summary>
/// One capability the assistant can invoke (create a task, look up the calendar, ...).
/// Register implementations in DI; <see cref="IToolRegistry"/> collects them.
/// </summary>
public interface ITool
{
    /// <summary>snake_case, stable - the model refers to the tool by this.</summary>
    string Name { get; }
    string Description { get; }
    /// <summary>JSON Schema for the arguments object.</summary>
    JsonObject ParametersSchema { get; }
    /// <summary>
    /// When true the orchestrator will not let the model run this without an explicit
    /// { "confirmed": true } argument - used for destructive actions (delete, send email).
    /// </summary>
    bool RequiresConfirmation { get; }
    Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken ct = default);
}

public record ToolExecutionContext(Guid UserId, string ConversationId, JsonObject Arguments, string TimeZone);

public record ToolExecutionResult(string Content, bool IsError = false)
{
    public static ToolExecutionResult Ok(string content) => new(content);
    public static ToolExecutionResult Error(string message) => new(message, true);
    /// <summary>Signals the model to ask the user before retrying with confirmed:true.</summary>
    public static ToolExecutionResult NeedsConfirmation(string details) =>
        new($"CONFIRMATION REQUIRED. Do not proceed until the user explicitly says yes. Details: {details}", false);
}

public interface IToolRegistry
{
    IReadOnlyList<ITool> All { get; }
    ITool? Find(string name);
    IReadOnlyList<LlmToolDefinition> ToLlmDefinitions();
}

/// <summary>Decides whether an exchange contains anything worth storing as long-term memory.</summary>
public interface IMemoryExtractor
{
    Task ExtractAndStoreAsync(Guid userId, string conversationId, string userText, string assistantText, CancellationToken ct = default);
}

public interface IOrchestrator
{
    Task<Dtos.ChatTurnResult> HandleAsync(Guid userId, Guid? conversationId, string userMessage, CancellationToken ct = default);
}

public interface IBriefingService
{
    Task<Dtos.DailyBriefing> BuildAsync(Guid userId, CancellationToken ct = default);
}
