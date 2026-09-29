using System.Text;
using System.Text.Json.Nodes;
using Assistant.Core.Abstractions;
using Assistant.Core.Entities;
using Assistant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Assistant.Infrastructure.Tools;

namespace Assistant.Infrastructure.Agents.Personal;

internal static class EventFormat
{
    public static string Line(CalendarEvent e) =>
        $"- {e.Title} (id={e.Id}) {e.StartsAt:yyyy-MM-dd HH:mm}Z-{e.EndsAt:HH:mm}Z"
        + (string.IsNullOrWhiteSpace(e.Location) ? "" : $" @ {e.Location}")
        + (string.IsNullOrWhiteSpace(e.Description) ? "" : $"\n    {e.Description}");
}

public class CreateEventTool(AssistantDbContext db) : ITool
{
    public string Name => "create_event";
    public string Description => "Add a calendar event. Always check for conflicts with find_free_time or get_events first when the user picks a time.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("title", "Event title", required: true)
        .Str("starts_at", "Start date/time, ISO-8601. Bare local times use the user's timezone.", required: true)
        .Str("ends_at", "End date/time, ISO-8601. Defaults to one hour after start.")
        .Str("location", "Optional location")
        .Str("description", "Optional description")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var title = ctx.Arguments.GetString("title");
        var start = ctx.Arguments.GetDateTimeUtc("starts_at", ctx.TimeZone);
        if (string.IsNullOrWhiteSpace(title) || start is null)
            return ToolExecutionResult.Error("title and a parseable starts_at are required");

        var end = ctx.Arguments.GetDateTimeUtc("ends_at", ctx.TimeZone) ?? start.Value.AddHours(1);
        if (end <= start) end = start.Value.AddHours(1);

        var conflicts = await db.Events
            .Where(e => e.UserId == ctx.UserId && e.StartsAt < end && e.EndsAt > start)
            .ToListAsync(ct);

        var ev = new CalendarEvent
        {
            UserId = ctx.UserId,
            Title = title.Trim(),
            StartsAt = start.Value,
            EndsAt = end,
            Location = ctx.Arguments.GetString("location"),
            Description = ctx.Arguments.GetString("description"),
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);

        var msg = new StringBuilder($"Created event.\n{EventFormat.Line(ev)}");
        if (conflicts.Count > 0)
        {
            msg.AppendLine("\nNOTE: this overlaps existing events - tell the user:");
            foreach (var c in conflicts) msg.AppendLine(EventFormat.Line(c));
        }
        return ToolExecutionResult.Ok(msg.ToString());
    }
}

public class GetEventsTool(AssistantDbContext db) : ITool
{
    public string Name => "get_events";
    public string Description => "List calendar events in a date range (defaults to the next 7 days).";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("from", "Range start (ISO-8601). Defaults to now.")
        .Str("to", "Range end (ISO-8601). Defaults to 7 days from start.")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var from = ctx.Arguments.GetDateTimeUtc("from", ctx.TimeZone) ?? DateTime.UtcNow;
        var to = ctx.Arguments.GetDateTimeUtc("to", ctx.TimeZone) ?? from.AddDays(7);

        var events = await db.Events
            .Where(e => e.UserId == ctx.UserId && e.StartsAt < to && e.EndsAt > from)
            .OrderBy(e => e.StartsAt).ToListAsync(ct);

        if (events.Count == 0) return ToolExecutionResult.Ok("No events in that range.");
        var sb = new StringBuilder($"{events.Count} event(s):\n");
        foreach (var e in events) sb.AppendLine(EventFormat.Line(e));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class UpdateEventTool(AssistantDbContext db) : ITool
{
    public string Name => "update_event";
    public string Description => "Change fields of an existing calendar event.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Event id (GUID)", required: true)
        .Str("title", "New title")
        .Str("starts_at", "New start (ISO-8601)")
        .Str("ends_at", "New end (ISO-8601)")
        .Str("location", "New location")
        .Str("description", "New description")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid event id is required");
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id && e.UserId == ctx.UserId, ct);
        if (ev is null) return ToolExecutionResult.Error("event not found");

        if (ctx.Arguments.GetString("title") is { } t && !string.IsNullOrWhiteSpace(t)) ev.Title = t.Trim();
        if (ctx.Arguments.GetDateTimeUtc("starts_at", ctx.TimeZone) is { } s) ev.StartsAt = s;
        if (ctx.Arguments.GetDateTimeUtc("ends_at", ctx.TimeZone) is { } e2) ev.EndsAt = e2;
        if (ctx.Arguments.GetString("location") is { } loc) ev.Location = loc;
        if (ctx.Arguments.GetString("description") is { } d) ev.Description = d;
        if (ev.EndsAt <= ev.StartsAt) ev.EndsAt = ev.StartsAt.AddHours(1);
        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Updated event.\n{EventFormat.Line(ev)}");
    }
}

public class DeleteEventTool(AssistantDbContext db) : ITool
{
    public string Name => "delete_event";
    public string Description => "Permanently delete a calendar event. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Event id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid event id is required");
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id && e.UserId == ctx.UserId, ct);
        if (ev is null) return ToolExecutionResult.Error("event not found");
        db.Events.Remove(ev);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted event: {ev.Title}");
    }
}

public class FindFreeTimeTool(AssistantDbContext db) : ITool
{
    public string Name => "find_free_time";
    public string Description => "Find open slots of a given length within working hours over a date range.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("from", "Range start (ISO-8601). Defaults to now.")
        .Str("to", "Range end (ISO-8601). Defaults to 3 days from start.")
        .Int("minutes", "Desired slot length in minutes (default 60)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var from = ctx.Arguments.GetDateTimeUtc("from", ctx.TimeZone) ?? DateTime.UtcNow;
        var to = ctx.Arguments.GetDateTimeUtc("to", ctx.TimeZone) ?? from.AddDays(3);
        var slot = TimeSpan.FromMinutes(Math.Clamp(ctx.Arguments.GetInt("minutes") ?? 60, 15, 480));

        var events = await db.Events
            .Where(e => e.UserId == ctx.UserId && e.StartsAt < to && e.EndsAt > from)
            .OrderBy(e => e.StartsAt).ToListAsync(ct);

        var free = new List<string>();
        var cursor = from;
        foreach (var e in events)
        {
            if (e.StartsAt - cursor >= slot)
                free.Add($"{cursor:yyyy-MM-dd HH:mm}Z - {e.StartsAt:HH:mm}Z");
            if (e.EndsAt > cursor) cursor = e.EndsAt;
            if (free.Count >= 8) break;
        }
        if (free.Count < 8 && to - cursor >= slot)
            free.Add($"{cursor:yyyy-MM-dd HH:mm}Z - {to:yyyy-MM-dd HH:mm}Z");

        return free.Count == 0
            ? ToolExecutionResult.Ok("No open slots of that length in the range.")
            : ToolExecutionResult.Ok("Open slots:\n" + string.Join("\n", free.Select(f => "- " + f)));
    }
}
