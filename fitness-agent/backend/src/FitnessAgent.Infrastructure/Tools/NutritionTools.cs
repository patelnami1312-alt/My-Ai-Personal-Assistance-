using System.Text;
using System.Text.Json.Nodes;
using FitnessAgent.Core.Abstractions;
using FitnessAgent.Core.Entities;
using FitnessAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessAgent.Infrastructure.Tools;

public class LogMealTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "log_meal";
    public string Description => "Record a meal / food intake entry.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("description", "What was eaten, e.g. 'Grilled chicken salad'", required: true)
        .Int("calories", "Estimated calories")
        .Num("protein_g", "Protein in grams")
        .Num("carbs_g", "Carbs in grams")
        .Num("fat_g", "Fat in grams")
        .Str("date", "ISO-8601 date/time. Defaults to now.")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var desc = ctx.Arguments.GetString("description");
        if (string.IsNullOrWhiteSpace(desc)) return ToolExecutionResult.Error("description is required");

        var n = new NutritionEntry
        {
            Description = desc.Trim(),
            Calories = ctx.Arguments.GetInt("calories"),
            ProteinG = ctx.Arguments.GetDouble("protein_g"),
            CarbsG = ctx.Arguments.GetDouble("carbs_g"),
            FatG = ctx.Arguments.GetDouble("fat_g"),
            Date = ctx.Arguments.GetDateUtc("date") ?? DateTime.UtcNow,
        };
        db.Nutrition.Add(n);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Logged meal: {n.Description}" + (n.Calories is { } c ? $" ({c} kcal)" : "") + $" (id={n.Id})");
    }
}

public class GetMealsTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "get_meals";
    public string Description => "List logged meals, optionally filtered by date range.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("from", "Range start (ISO-8601 date)")
        .Str("to", "Range end (ISO-8601 date)")
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Nutrition.AsQueryable();
        if (ctx.Arguments.GetDateUtc("from") is { } from) q = q.Where(n => n.Date >= from);
        if (ctx.Arguments.GetDateUtc("to") is { } to) q = q.Where(n => n.Date <= to);

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 100);
        var rows = await q.OrderByDescending(n => n.Date).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching meals.");

        var sb = new StringBuilder($"{rows.Count} meal(s):\n");
        foreach (var n in rows)
            sb.AppendLine($"- {n.Date:yyyy-MM-dd HH:mm} {n.Description} (id={n.Id})" +
                          (n.Calories is { } c ? $" - {c} kcal" : "") +
                          (n.ProteinG is { } p ? $", P{p}g" : "") +
                          (n.CarbsG is { } cb ? $" C{cb}g" : "") +
                          (n.FatG is { } f ? $" F{f}g" : ""));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class DeleteMealTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "delete_meal";
    public string Description => "Permanently delete a logged meal. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Meal id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid meal id is required");
        var n = await db.Nutrition.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null) return ToolExecutionResult.Error("meal not found");
        db.Nutrition.Remove(n);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted meal: {n.Description}");
    }
}
