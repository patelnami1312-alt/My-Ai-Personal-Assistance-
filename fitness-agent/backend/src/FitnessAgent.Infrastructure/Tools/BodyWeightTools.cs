using System.Text;
using System.Text.Json.Nodes;
using FitnessAgent.Core.Abstractions;
using FitnessAgent.Core.Entities;
using FitnessAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessAgent.Infrastructure.Tools;

public class LogBodyWeightTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "log_bodyweight";
    public string Description => "Record a body weight measurement.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Num("weight_kg", "Weight in kg", required: true)
        .Str("notes", "Optional notes")
        .Str("date", "ISO-8601 date/time. Defaults to now.")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var kg = ctx.Arguments.GetDouble("weight_kg");
        if (kg is null) return ToolExecutionResult.Error("weight_kg is required");

        var e = new BodyWeightEntry
        {
            WeightKg = kg.Value,
            Notes = ctx.Arguments.GetString("notes"),
            Date = ctx.Arguments.GetDateUtc("date") ?? DateTime.UtcNow,
        };
        db.BodyWeights.Add(e);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Logged weight: {e.WeightKg}kg on {e.Date:yyyy-MM-dd} (id={e.Id})");
    }
}

public class GetBodyWeightHistoryTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "get_bodyweight_history";
    public string Description => "List body weight history, most recent first.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 200);
        var rows = await db.BodyWeights.OrderByDescending(w => w.Date).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No weight entries yet.");

        var sb = new StringBuilder($"{rows.Count} entr(ies), most recent first:\n");
        foreach (var w in rows)
            sb.AppendLine($"- {w.Date:yyyy-MM-dd}: {w.WeightKg}kg (id={w.Id})" + (string.IsNullOrWhiteSpace(w.Notes) ? "" : $" - {w.Notes}"));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class DeleteBodyWeightEntryTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "delete_bodyweight_entry";
    public string Description => "Permanently delete a body weight entry. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Entry id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid entry id is required");
        var w = await db.BodyWeights.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (w is null) return ToolExecutionResult.Error("entry not found");
        db.BodyWeights.Remove(w);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted weight entry: {w.WeightKg}kg on {w.Date:yyyy-MM-dd}");
    }
}
