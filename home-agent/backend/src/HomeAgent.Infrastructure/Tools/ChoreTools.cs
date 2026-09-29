using System.Text;
using System.Text.Json.Nodes;
using HomeAgent.Core.Abstractions;
using HomeAgent.Core.Entities;
using HomeAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeAgent.Infrastructure.Tools;

internal static class ChoreFormat
{
    public static string Line(Chore c) =>
        $"- {c.Title} (id={c.Id}) [{c.Status}]" +
        (c.AssignedTo is { } a && !string.IsNullOrWhiteSpace(a) ? $" - assigned to {a}" : "") +
        (c.DueDate is { } d ? $" - due {d:yyyy-MM-dd}" : "") +
        (c.Recurring ? $" - recurring ({c.Frequency})" : "") +
        (string.IsNullOrWhiteSpace(c.Notes) ? "" : $"\n    {c.Notes}");

    public static DateTime Advance(DateTime from, RecurrenceFrequency freq) => freq switch
    {
        RecurrenceFrequency.Daily => from.AddDays(1),
        RecurrenceFrequency.Weekly => from.AddDays(7),
        RecurrenceFrequency.Monthly => from.AddMonths(1),
        _ => from,
    };

    public static bool TryParseFrequency(string? raw, out RecurrenceFrequency freq)
    {
        freq = RecurrenceFrequency.OneTime;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return Enum.TryParse(raw, true, out freq);
    }
}

public class AddChoreTool(HomeAgentDbContext db) : ITool
{
    public string Name => "add_chore";
    public string Description => "Add a new household chore, optionally recurring.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("title", "Chore title, e.g. 'Take out the trash'", required: true)
        .Str("assigned_to", "Who the chore is assigned to")
        .Str("due_date", "ISO-8601 due date/time")
        .Bool("recurring", "Whether this chore repeats")
        .Str("frequency", "Recurrence frequency", enumValues: ["OneTime", "Daily", "Weekly", "Monthly"])
        .Str("notes", "Optional notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var title = ctx.Arguments.GetString("title");
        if (string.IsNullOrWhiteSpace(title)) return ToolExecutionResult.Error("title is required");

        ChoreFormat.TryParseFrequency(ctx.Arguments.GetString("frequency"), out var freq);
        var recurring = ctx.Arguments.GetBool("recurring", freq != RecurrenceFrequency.OneTime);

        var c = new Chore
        {
            Title = title.Trim(),
            AssignedTo = ctx.Arguments.GetString("assigned_to"),
            DueDate = ctx.Arguments.GetDateUtc("due_date"),
            Recurring = recurring,
            Frequency = freq,
            Notes = ctx.Arguments.GetString("notes"),
        };
        db.Chores.Add(c);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Added chore.\n{ChoreFormat.Line(c)}");
    }
}

public class GetChoresTool(HomeAgentDbContext db) : ITool
{
    public string Name => "get_chores";
    public string Description => "List chores, optionally filtered by status, assignee, or due today.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("status", "Filter by status", enumValues: ["Todo", "Done"])
        .Str("assigned_to", "Filter by assignee (partial match)")
        .Bool("due_today", "Only chores due today")
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Chores.AsQueryable();
        if (Enum.TryParse<ChoreStatus>(ctx.Arguments.GetString("status"), true, out var status))
            q = q.Where(c => c.Status == status);
        if (ctx.Arguments.GetString("assigned_to") is { } who && !string.IsNullOrWhiteSpace(who))
            q = q.Where(c => c.AssignedTo != null && EF.Functions.ILike(c.AssignedTo, $"%{who}%"));
        if (ctx.Arguments.GetBool("due_today"))
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            q = q.Where(c => c.DueDate != null && c.DueDate >= today && c.DueDate < tomorrow);
        }

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 100);
        var rows = await q.OrderBy(c => c.DueDate == null).ThenBy(c => c.DueDate).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching chores.");

        var sb = new StringBuilder($"{rows.Count} chore(s):\n");
        foreach (var c in rows) sb.AppendLine(ChoreFormat.Line(c));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class CompleteChoreTool(HomeAgentDbContext db) : ITool
{
    public string Name => "complete_chore";
    public string Description => "Mark a chore as done. If it is recurring, the next occurrence is created automatically.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Chore id (GUID)", required: true)
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid chore id is required");
        var c = await db.Chores.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return ToolExecutionResult.Error("chore not found");

        c.Status = ChoreStatus.Done;
        c.CompletedAt = DateTime.UtcNow;

        var msg = new StringBuilder($"Completed chore: {c.Title}");

        if (c.Recurring && c.Frequency != RecurrenceFrequency.OneTime)
        {
            var baseDate = c.DueDate ?? DateTime.UtcNow;
            var next = new Chore
            {
                Title = c.Title,
                AssignedTo = c.AssignedTo,
                DueDate = ChoreFormat.Advance(baseDate, c.Frequency),
                Recurring = true,
                Frequency = c.Frequency,
                Status = ChoreStatus.Todo,
                Notes = c.Notes,
            };
            db.Chores.Add(next);
            msg.Append($". Next occurrence created, due {next.DueDate:yyyy-MM-dd} (id={next.Id})");
        }

        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok(msg.ToString());
    }
}

public class DeleteChoreTool(HomeAgentDbContext db) : ITool
{
    public string Name => "delete_chore";
    public string Description => "Permanently delete a chore. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Chore id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid chore id is required");
        var c = await db.Chores.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return ToolExecutionResult.Error("chore not found");
        db.Chores.Remove(c);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted chore: {c.Title}");
    }
}
