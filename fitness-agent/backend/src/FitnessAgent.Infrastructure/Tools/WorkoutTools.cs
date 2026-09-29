using System.Text;
using System.Text.Json.Nodes;
using FitnessAgent.Core.Abstractions;
using FitnessAgent.Core.Entities;
using FitnessAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessAgent.Infrastructure.Tools;

internal static class WorkoutFormat
{
    public static string Line(WorkoutEntry w) =>
        $"- {w.Date:yyyy-MM-dd} {w.Exercise} (id={w.Id})" +
        (w.Sets is { } s && w.Reps is { } r ? $" - {s}x{r}" : "") +
        (w.WeightKg is { } kg ? $" @ {kg}kg" : "") +
        (w.DurationMinutes is { } d ? $", {d} min" : "") +
        (string.IsNullOrWhiteSpace(w.Notes) ? "" : $"\n    {w.Notes}");
}

public class LogWorkoutTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "log_workout";
    public string Description => "Record a workout / exercise session.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("exercise", "Exercise name, e.g. 'Bench press' or 'Running'", required: true)
        .Int("sets", "Number of sets")
        .Int("reps", "Reps per set")
        .Num("weight_kg", "Weight used, in kg")
        .Int("duration_minutes", "Duration in minutes (for cardio or timed workouts)")
        .Str("notes", "Optional notes")
        .Str("date", "ISO-8601 date/time. Defaults to now.")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var exercise = ctx.Arguments.GetString("exercise");
        if (string.IsNullOrWhiteSpace(exercise)) return ToolExecutionResult.Error("exercise is required");

        var w = new WorkoutEntry
        {
            Exercise = exercise.Trim(),
            Sets = ctx.Arguments.GetInt("sets"),
            Reps = ctx.Arguments.GetInt("reps"),
            WeightKg = ctx.Arguments.GetDouble("weight_kg"),
            DurationMinutes = ctx.Arguments.GetInt("duration_minutes"),
            Notes = ctx.Arguments.GetString("notes"),
            Date = ctx.Arguments.GetDateUtc("date") ?? DateTime.UtcNow,
        };
        db.Workouts.Add(w);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Logged workout.\n{WorkoutFormat.Line(w)}");
    }
}

public class GetWorkoutsTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "get_workouts";
    public string Description => "List logged workouts, optionally filtered by date range or exercise.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("from", "Range start (ISO-8601 date)")
        .Str("to", "Range end (ISO-8601 date)")
        .Str("exercise", "Filter by exercise name (partial match)")
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Workouts.AsQueryable();
        if (ctx.Arguments.GetDateUtc("from") is { } from) q = q.Where(w => w.Date >= from);
        if (ctx.Arguments.GetDateUtc("to") is { } to) q = q.Where(w => w.Date <= to);
        if (ctx.Arguments.GetString("exercise") is { } ex && !string.IsNullOrWhiteSpace(ex))
            q = q.Where(w => EF.Functions.ILike(w.Exercise, $"%{ex}%"));

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 100);
        var rows = await q.OrderByDescending(w => w.Date).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching workouts.");

        var sb = new StringBuilder($"{rows.Count} workout(s):\n");
        foreach (var w in rows) sb.AppendLine(WorkoutFormat.Line(w));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class DeleteWorkoutTool(FitnessAgentDbContext db) : ITool
{
    public string Name => "delete_workout";
    public string Description => "Permanently delete a logged workout. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Workout id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid workout id is required");
        var w = await db.Workouts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (w is null) return ToolExecutionResult.Error("workout not found");
        db.Workouts.Remove(w);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted workout: {w.Exercise} on {w.Date:yyyy-MM-dd}");
    }
}
