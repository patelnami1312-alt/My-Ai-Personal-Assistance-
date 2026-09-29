using System.Text.Json.Nodes;
using FinanceAgent.Core.Llm;

namespace FinanceAgent.Core.Abstractions;

public interface ILlmProvider
{
    string Name { get; }
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default);
}

public interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonObject ParametersSchema { get; }
    bool RequiresConfirmation { get; }
    Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken ct = default);
}

public record ToolExecutionContext(JsonObject Arguments);

public record ToolExecutionResult(string Content, bool IsError = false)
{
    public static ToolExecutionResult Ok(string content) => new(content);
    public static ToolExecutionResult Error(string message) => new(message, true);
    public static ToolExecutionResult NeedsConfirmation(string details) =>
        new($"CONFIRMATION REQUIRED. Do not proceed until the user explicitly says yes. Details: {details}", false);
}

public interface IToolRegistry
{
    IReadOnlyList<ITool> All { get; }
    ITool? Find(string name);
    IReadOnlyList<LlmToolDefinition> ToLlmDefinitions();
}

public interface IOrchestrator
{
    Task<Core.Dtos.ChatTurnResult> HandleAsync(Guid? conversationId, string userMessage, CancellationToken ct = default);
}
