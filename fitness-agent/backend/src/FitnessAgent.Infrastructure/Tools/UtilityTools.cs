using System.Text.Json.Nodes;
using FitnessAgent.Core.Abstractions;

namespace FitnessAgent.Infrastructure.Tools;

public class DateTimeTool : ITool
{
    public string Name => "current_datetime";
    public string Description => "Get the current UTC date and time. Use this before reasoning about relative dates like 'today' or 'this week'.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object().Build();

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default) =>
        Task.FromResult(ToolExecutionResult.Ok($"UTC: {DateTime.UtcNow:dddd, yyyy-MM-dd HH:mm}Z"));
}
