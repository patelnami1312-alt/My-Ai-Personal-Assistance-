using System.Data;
using System.Text.Json.Nodes;
using Assistant.Core.Abstractions;

using Assistant.Infrastructure.Tools;

namespace Assistant.Infrastructure.Agents.Personal;

public class DateTimeTool : ITool
{
    public string Name => "current_datetime";
    public string Description => "Get the current date and time, both in UTC and the user's timezone. Use this before reasoning about relative dates like 'tomorrow'.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object().Build();

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var utc = DateTime.UtcNow;
        string local;
        try
        {
            var tzi = TimeZoneInfo.FindSystemTimeZoneById(ctx.TimeZone);
            var lt = TimeZoneInfo.ConvertTimeFromUtc(utc, tzi);
            local = $"{lt:dddd, yyyy-MM-dd HH:mm} ({ctx.TimeZone})";
        }
        catch
        {
            local = "(unknown timezone)";
        }
        return Task.FromResult(ToolExecutionResult.Ok(
            $"UTC: {utc:dddd, yyyy-MM-dd HH:mm}Z\nUser local: {local}"));
    }
}

public class CalculatorTool : ITool
{
    public string Name => "calculator";
    public string Description => "Evaluate a basic arithmetic expression (+ - * / parentheses, %).";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("expression", "e.g. '(12000 * 1.08) / 3'", required: true)
        .Build();

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var expr = ctx.Arguments.GetString("expression");
        if (string.IsNullOrWhiteSpace(expr)) return Task.FromResult(ToolExecutionResult.Error("expression is required"));
        try
        {
            using var dt = new DataTable();
            var result = dt.Compute(expr, null);
            return Task.FromResult(ToolExecutionResult.Ok($"{expr} = {result}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolExecutionResult.Error($"could not evaluate: {ex.Message}"));
        }
    }
}

/// <summary>
/// Placeholder so the model knows web search exists. Wire a real provider (Brave, Tavily,
/// or the Anthropic server-side web_search tool) in Step 11.
/// </summary>
public class WebSearchTool : ITool
{
    public string Name => "web_search";
    public string Description => "Search the web for current information. (Not configured yet in this build.)";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("query", "Search query", required: true)
        .Build();

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
        => Task.FromResult(ToolExecutionResult.Error(
            "web_search is not configured in this build. Tell the user web search will be added later and answer from your own knowledge, noting it may be out of date."));
}
