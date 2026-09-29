using System.Text;
using System.Text.Json.Nodes;
using HomeAgent.Core.Abstractions;
using HomeAgent.Core.Entities;
using HomeAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeAgent.Infrastructure.Tools;

internal static class MaintenanceFormat
{
    public static string Line(MaintenanceItem m) =>
        $"- {m.Title} (id={m.Id})" +
        (m.LastDone is { } l ? $" - last done {l:yyyy-MM-dd}" : "") +
        (m.IntervalDays is { } iv ? $" - every {iv} days" : "") +
        (m.NextDue is { } n ? $" - next due {n:yyyy-MM-dd}" : "") +
        (string.IsNullOrWhiteSpace(m.Notes) ? "" : $"\n    {m.Notes}");
}

public class AddMaintenanceItemTool(HomeAgentDbContext db) : ITool
{
    public string Name => "add_maintenance_item";
    public string Description => "Add a recurring home maintenance item, e.g. 'Change HVAC filter'.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("title", "Maintenance item title, e.g. 'Change HVAC filter'", required: true)
        .Int("interval_days", "How often this needs to be redone, in days, e.g. 90")
        .Str("last_done", "ISO-8601 date it was last done")
        .Str("notes", "Optional notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var title = ctx.Arguments.GetString("title");
        if (string.IsNullOrWhiteSpace(title)) return ToolExecutionResult.Error("title is required");

        var intervalDays = ctx.Arguments.GetInt("interval_days");
        var lastDone = ctx.Arguments.GetDateUtc("last_done");

        var m = new MaintenanceItem
        {
            Title = title.Trim(),
            IntervalDays = intervalDays,
            LastDone = lastDone,
            NextDue = (lastDone is { } ld && intervalDays is { } iv) ? ld.AddDays(iv) : null,
            Notes = ctx.Arguments.GetString("notes"),
        };
        db.MaintenanceItems.Add(m);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Added maintenance item.\n{MaintenanceFormat.Line(m)}");
    }
}

public class GetMaintenanceItemsTool(HomeAgentDbContext db) : ITool
{
    public string Name => "get_maintenance_items";
    public string Description => "List home maintenance items, soonest due first.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Bool("due_soon", "Only items due within the next 30 days")
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.MaintenanceItems.AsQueryable();
        if (ctx.Arguments.GetBool("due_soon"))
        {
            var cutoff = DateTime.UtcNow.AddDays(30);
            q = q.Where(m => m.NextDue != null && m.NextDue <= cutoff);
        }

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 100);
        var rows = await q.OrderBy(m => m.NextDue == null).ThenBy(m => m.NextDue).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching maintenance items.");

        var sb = new StringBuilder($"{rows.Count} maintenance item(s):\n");
        foreach (var m in rows) sb.AppendLine(MaintenanceFormat.Line(m));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class MarkMaintenanceDoneTool(HomeAgentDbContext db) : ITool
{
    public string Name => "mark_maintenance_done";
    public string Description => "Mark a maintenance item as done today, and recompute its next due date if it has an interval.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Maintenance item id (GUID)", required: true)
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid maintenance item id is required");
        var m = await db.MaintenanceItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return ToolExecutionResult.Error("maintenance item not found");

        m.LastDone = DateTime.UtcNow;
        m.NextDue = m.IntervalDays is { } iv ? m.LastDone.Value.AddDays(iv) : null;
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Marked done: {m.Title}." +
            (m.NextDue is { } n ? $" Next due {n:yyyy-MM-dd}." : ""));
    }
}

public class DeleteMaintenanceItemTool(HomeAgentDbContext db) : ITool
{
    public string Name => "delete_maintenance_item";
    public string Description => "Permanently delete a maintenance item. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Maintenance item id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid maintenance item id is required");
        var m = await db.MaintenanceItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return ToolExecutionResult.Error("maintenance item not found");
        db.MaintenanceItems.Remove(m);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted maintenance item: {m.Title}");
    }
}
