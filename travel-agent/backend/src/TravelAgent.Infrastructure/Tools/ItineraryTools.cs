using System.Text;
using System.Text.Json.Nodes;
using TravelAgent.Core.Abstractions;
using TravelAgent.Core.Entities;
using TravelAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TravelAgent.Infrastructure.Tools;

internal static class ItineraryFormat
{
    public static string Line(ItineraryItem i) =>
        $"- {i.StartsAt:yyyy-MM-dd HH:mm} {i.Title} (id={i.Id}) [{i.Type}]" +
        (i.EndsAt is { } end ? $" until {end:yyyy-MM-dd HH:mm}" : "") +
        (string.IsNullOrWhiteSpace(i.Location) ? "" : $" @ {i.Location}") +
        (string.IsNullOrWhiteSpace(i.Notes) ? "" : $"\n    {i.Notes}");
}

public class AddItineraryItemTool(TravelAgentDbContext db) : ITool
{
    public string Name => "add_itinerary_item";
    public string Description => "Add an item (flight, hotel, activity, transport, other) to a trip's itinerary.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("trip_id", "Trip id (GUID) this item belongs to", required: true)
        .Str("title", "Short title, e.g. 'Flight to Tokyo'", required: true)
        .Str("type", "Item type", required: true, enumValues: ["Flight", "Hotel", "Activity", "Transport", "Other"])
        .Str("starts_at", "Start date/time (ISO-8601)", required: true)
        .Str("ends_at", "End date/time (ISO-8601)")
        .Str("location", "Location name or address")
        .Str("notes", "Optional notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("trip_id"), out var tripId))
            return ToolExecutionResult.Error("valid trip_id is required");
        var tripExists = await db.Trips.AnyAsync(t => t.Id == tripId, ct);
        if (!tripExists) return ToolExecutionResult.Error("trip not found - use get_trips to find the right trip_id");

        var title = ctx.Arguments.GetString("title");
        if (string.IsNullOrWhiteSpace(title)) return ToolExecutionResult.Error("title is required");

        var typeRaw = ctx.Arguments.GetString("type");
        if (!TripFormat.TryParseType(typeRaw, out var type))
            return ToolExecutionResult.Error("type must be one of Flight, Hotel, Activity, Transport, Other");

        var starts = ctx.Arguments.GetDateUtc("starts_at");
        if (starts is null) return ToolExecutionResult.Error("starts_at is required and must be a valid date/time");

        var item = new ItineraryItem
        {
            TripId = tripId,
            Title = title.Trim(),
            Type = type,
            StartsAt = starts.Value,
            EndsAt = ctx.Arguments.GetDateUtc("ends_at"),
            Location = ctx.Arguments.GetString("location"),
            Notes = ctx.Arguments.GetString("notes"),
        };
        db.ItineraryItems.Add(item);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Added itinerary item.\n{ItineraryFormat.Line(item)}");
    }
}

public class GetItineraryTool(TravelAgentDbContext db) : ITool
{
    public string Name => "get_itinerary";
    public string Description => "List itinerary items for a trip, in chronological order.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("trip_id", "Trip id (GUID)", required: true)
        .Int("limit", "Max rows (default 50)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("trip_id"), out var tripId))
            return ToolExecutionResult.Error("valid trip_id is required");

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 50, 1, 200);
        var rows = await db.ItineraryItems
            .Where(i => i.TripId == tripId)
            .OrderBy(i => i.StartsAt)
            .Take(limit)
            .ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No itinerary items for this trip.");

        var sb = new StringBuilder($"{rows.Count} itinerary item(s):\n");
        foreach (var i in rows) sb.AppendLine(ItineraryFormat.Line(i));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class DeleteItineraryItemTool(TravelAgentDbContext db) : ITool
{
    public string Name => "delete_itinerary_item";
    public string Description => "Permanently delete an itinerary item. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Itinerary item id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid itinerary item id is required");
        var i = await db.ItineraryItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i is null) return ToolExecutionResult.Error("itinerary item not found");
        db.ItineraryItems.Remove(i);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted itinerary item: {i.Title}");
    }
}
