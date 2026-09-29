using System.Text;
using System.Text.Json.Nodes;
using TravelAgent.Core.Abstractions;
using TravelAgent.Core.Entities;
using TravelAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TravelAgent.Infrastructure.Tools;

internal static class TripFormat
{
    public static string Line(Trip t) =>
        $"- {t.Destination} (id={t.Id}) {t.StartDate:yyyy-MM-dd} to {t.EndDate:yyyy-MM-dd} [{t.Status}]" +
        (string.IsNullOrWhiteSpace(t.Notes) ? "" : $"\n    {t.Notes}");

    public static bool TryParseStatus(string? raw, out TripStatus status)
        => Enum.TryParse(raw, true, out status);

    public static bool TryParseType(string? raw, out ItineraryItemType type)
        => Enum.TryParse(raw, true, out type);
}

public class CreateTripTool(TravelAgentDbContext db) : ITool
{
    public string Name => "create_trip";
    public string Description => "Create a new trip to plan.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("destination", "Trip destination, e.g. 'Tokyo, Japan'", required: true)
        .Str("start_date", "Trip start date (ISO-8601)", required: true)
        .Str("end_date", "Trip end date (ISO-8601)", required: true)
        .Str("notes", "Optional notes about the trip")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var destination = ctx.Arguments.GetString("destination");
        if (string.IsNullOrWhiteSpace(destination)) return ToolExecutionResult.Error("destination is required");
        var start = ctx.Arguments.GetDateUtc("start_date");
        var end = ctx.Arguments.GetDateUtc("end_date");
        if (start is null) return ToolExecutionResult.Error("start_date is required and must be a valid date");
        if (end is null) return ToolExecutionResult.Error("end_date is required and must be a valid date");

        var trip = new Trip
        {
            Destination = destination.Trim(),
            StartDate = start.Value,
            EndDate = end.Value,
            Status = TripStatus.Planned,
            Notes = ctx.Arguments.GetString("notes"),
        };
        db.Trips.Add(trip);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Created trip.\n{TripFormat.Line(trip)}");
    }
}

public class GetTripsTool(TravelAgentDbContext db) : ITool
{
    public string Name => "get_trips";
    public string Description => "List trips, optionally filtered by status. Soonest start date first.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("status", "Filter by status", enumValues: ["Planned", "Ongoing", "Completed"])
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Trips.AsQueryable();
        var statusRaw = ctx.Arguments.GetString("status");
        if (!string.IsNullOrWhiteSpace(statusRaw))
        {
            if (!TripFormat.TryParseStatus(statusRaw, out var status))
                return ToolExecutionResult.Error("status must be one of Planned, Ongoing, Completed");
            q = q.Where(t => t.Status == status);
        }

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 100);
        var rows = await q.OrderBy(t => t.StartDate).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching trips.");

        var sb = new StringBuilder($"{rows.Count} trip(s):\n");
        foreach (var t in rows) sb.AppendLine(TripFormat.Line(t));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class UpdateTripTool(TravelAgentDbContext db) : ITool
{
    public string Name => "update_trip";
    public string Description => "Update fields on an existing trip. Not destructive.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Trip id (GUID)", required: true)
        .Str("destination", "New destination")
        .Str("start_date", "New start date (ISO-8601)")
        .Str("end_date", "New end date (ISO-8601)")
        .Str("status", "New status", enumValues: ["Planned", "Ongoing", "Completed"])
        .Str("notes", "New notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid trip id is required");
        var t = await db.Trips.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return ToolExecutionResult.Error("trip not found");

        if (ctx.Arguments.GetString("destination") is { } dest && !string.IsNullOrWhiteSpace(dest)) t.Destination = dest.Trim();
        if (ctx.Arguments.GetDateUtc("start_date") is { } start) t.StartDate = start;
        if (ctx.Arguments.GetDateUtc("end_date") is { } end) t.EndDate = end;
        if (ctx.Arguments.GetString("status") is { } statusRaw && !string.IsNullOrWhiteSpace(statusRaw))
        {
            if (!TripFormat.TryParseStatus(statusRaw, out var status))
                return ToolExecutionResult.Error("status must be one of Planned, Ongoing, Completed");
            t.Status = status;
        }
        if (ctx.Arguments.GetString("notes") is { } notes) t.Notes = notes;

        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Updated trip.\n{TripFormat.Line(t)}");
    }
}

public class DeleteTripTool(TravelAgentDbContext db) : ITool
{
    public string Name => "delete_trip";
    public string Description => "Permanently delete a trip and its itinerary/packing items. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Trip id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid trip id is required");
        var t = await db.Trips.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return ToolExecutionResult.Error("trip not found");
        db.Trips.Remove(t);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted trip: {t.Destination} ({t.StartDate:yyyy-MM-dd} to {t.EndDate:yyyy-MM-dd})");
    }
}
