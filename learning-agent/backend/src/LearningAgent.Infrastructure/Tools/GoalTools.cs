using System.Text;
using System.Text.Json.Nodes;
using LearningAgent.Core.Abstractions;
using LearningAgent.Core.Entities;
using LearningAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LearningAgent.Infrastructure.Tools;

internal static class LearningGoalFormat
{
    public static string Line(LearningGoal g) =>
        $"- [{g.Status}] {g.Description} (id={g.Id}" +
        (g.TargetDate is { } d ? $", by {d:yyyy-MM-dd}" : "") + ")";
}

public class SetLearningGoalTool(LearningAgentDbContext db) : ITool
{
    public string Name => "set_learning_goal";
    public string Description => "Create a new learning goal.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("description", "Human-readable goal, e.g. 'Finish the React course'", required: true)
        .Str("target_date", "Optional target date (ISO-8601)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var desc = ctx.Arguments.GetString("description");
        if (string.IsNullOrWhiteSpace(desc)) return ToolExecutionResult.Error("description is required");

        var g = new LearningGoal
        {
            Description = desc.Trim(),
            TargetDate = ctx.Arguments.GetDateUtc("target_date"),
            Status = LearningGoalStatus.Active,
        };
        db.Goals.Add(g);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Goal set.\n{LearningGoalFormat.Line(g)}");
    }
}

public class GetLearningGoalsTool(LearningAgentDbContext db) : ITool
{
    public string Name => "get_learning_goals";
    public string Description => "List learning goals, optionally filtered by status.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("status", "Filter by status", enumValues: ["Active", "Achieved", "Abandoned"])
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Goals.AsQueryable();
        if (Enum.TryParse<LearningGoalStatus>(ctx.Arguments.GetString("status"), true, out var s))
            q = q.Where(g => g.Status == s);

        var rows = await q.OrderBy(g => g.Status).ThenByDescending(g => g.CreatedAt).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching goals.");

        var sb = new StringBuilder($"{rows.Count} goal(s):\n");
        foreach (var g in rows) sb.AppendLine(LearningGoalFormat.Line(g));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class UpdateLearningGoalTool(LearningAgentDbContext db) : ITool
{
    public string Name => "update_learning_goal";
    public string Description => "Update a learning goal's status or target date, e.g. mark it Achieved.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Goal id (GUID)", required: true)
        .Str("status", "New status", enumValues: ["Active", "Achieved", "Abandoned"])
        .Str("target_date", "New target date (ISO-8601)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid goal id is required");
        var g = await db.Goals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return ToolExecutionResult.Error("goal not found");

        if (Enum.TryParse<LearningGoalStatus>(ctx.Arguments.GetString("status"), true, out var s)) g.Status = s;
        if (ctx.Arguments.GetDateUtc("target_date") is { } td) g.TargetDate = td;

        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Updated goal.\n{LearningGoalFormat.Line(g)}");
    }
}

public class DeleteLearningGoalTool(LearningAgentDbContext db) : ITool
{
    public string Name => "delete_learning_goal";
    public string Description => "Permanently delete a learning goal. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Goal id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid goal id is required");
        var g = await db.Goals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return ToolExecutionResult.Error("goal not found");
        db.Goals.Remove(g);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted goal: {g.Description}");
    }
}
