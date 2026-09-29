using System.Text;
using System.Text.Json.Nodes;
using FitnessAgent.Core.Abstractions;
using FitnessAgent.Core.Entities;
using FitnessAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessAgent.Infrastructure.Tools;

internal static class GoalFormat
{
    public static string Line(FitnessGoal g) =>
        $"- [{g.Status}] {g.Description} (id={g.Id}, type={g.Type}" +
        (g.TargetValue is { } t ? $", target={t}" : "") +
        (g.TargetDate is { } d ? $", by {d:yyyy-MM-dd}" : "") + ")";
}

public class SetGoalTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "set_goal";
    public string Description => "Create a new fitness goal (weight target, workout frequency, or a custom goal).";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("description", "Human-readable goal, e.g. 'Reach 70kg' or 'Work out 4x/week'", required: true)
        .Str("type", "Goal type", enumValues: ["WeightTarget", "WorkoutFrequency", "Custom"])
        .Num("target_value", "Numeric target - kg for WeightTarget, sessions/week for WorkoutFrequency")
        .Str("target_date", "Optional target date (ISO-8601)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var desc = ctx.Arguments.GetString("description");
        if (string.IsNullOrWhiteSpace(desc)) return ToolExecutionResult.Error("description is required");

        var g = new FitnessGoal
        {
            Description = desc.Trim(),
            Type = Enum.TryParse<GoalType>(ctx.Arguments.GetString("type"), true, out var t) ? t : GoalType.Custom,
            TargetValue = ctx.Arguments.GetDouble("target_value"),
            TargetDate = ctx.Arguments.GetDateUtc("target_date"),
        };
        db.Goals.Add(g);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Goal set.\n{GoalFormat.Line(g)}");
    }
}

public class GetGoalsTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "get_goals";
    public string Description => "List fitness goals, optionally filtered by status.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("status", "Filter by status", enumValues: ["Active", "Achieved", "Abandoned"])
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Goals.AsQueryable();
        if (Enum.TryParse<GoalStatus>(ctx.Arguments.GetString("status"), true, out var s))
            q = q.Where(g => g.Status == s);

        var rows = await q.OrderBy(g => g.Status).ThenByDescending(g => g.CreatedAt).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching goals.");

        var sb = new StringBuilder($"{rows.Count} goal(s):\n");
        foreach (var g in rows) sb.AppendLine(GoalFormat.Line(g));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class UpdateGoalTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "update_goal";
    public string Description => "Update a goal's status or details, e.g. mark it Achieved.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Goal id (GUID)", required: true)
        .Str("status", "New status", enumValues: ["Active", "Achieved", "Abandoned"])
        .Num("target_value", "New target value")
        .Str("target_date", "New target date (ISO-8601)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid goal id is required");
        var g = await db.Goals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return ToolExecutionResult.Error("goal not found");

        if (Enum.TryParse<GoalStatus>(ctx.Arguments.GetString("status"), true, out var s)) g.Status = s;
        if (ctx.Arguments.GetDouble("target_value") is { } tv) g.TargetValue = tv;
        if (ctx.Arguments.GetDateUtc("target_date") is { } td) g.TargetDate = td;

        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Updated goal.\n{GoalFormat.Line(g)}");
    }
}

public class DeleteGoalTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "delete_goal";
    public string Description => "Permanently delete a goal. Destructive - the user must confirm first.";
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
