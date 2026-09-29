using System.Text;
using System.Text.Json.Nodes;
using TravelAgent.Core.Abstractions;
using TravelAgent.Core.Entities;
using TravelAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TravelAgent.Infrastructure.Tools;

internal static class PackingFormat
{
    public static string Line(PackingItem p) =>
        $"- [{(p.Packed ? "x" : " ")}] {p.Name} (id={p.Id})" +
        (string.IsNullOrWhiteSpace(p.Category) ? "" : $" - {p.Category}");
}

public class AddPackingItemTool(TravelAgentDbContext db) : ITool
{
    public string Name => "add_packing_item";
    public string Description => "Add an item to a trip's packing list.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("trip_id", "Trip id (GUID) this item belongs to", required: true)
        .Str("name", "Item name, e.g. 'Passport'", required: true)
        .Str("category", "Optional category, e.g. 'Documents', 'Clothing'")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("trip_id"), out var tripId))
            return ToolExecutionResult.Error("valid trip_id is required");
        var tripExists = await db.Trips.AnyAsync(t => t.Id == tripId, ct);
        if (!tripExists) return ToolExecutionResult.Error("trip not found - use get_trips to find the right trip_id");

        var name = ctx.Arguments.GetString("name");
        if (string.IsNullOrWhiteSpace(name)) return ToolExecutionResult.Error("name is required");

        var item = new PackingItem
        {
            TripId = tripId,
            Name = name.Trim(),
            Category = ctx.Arguments.GetString("category"),
        };
        db.PackingItems.Add(item);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Added packing item.\n{PackingFormat.Line(item)}");
    }
}

public class GetPackingListTool(TravelAgentDbContext db) : ITool
{
    public string Name => "get_packing_list";
    public string Description => "List packing items for a trip, optionally filtered by packed status.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("trip_id", "Trip id (GUID)", required: true)
        .Bool("packed", "Filter by packed status")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("trip_id"), out var tripId))
            return ToolExecutionResult.Error("valid trip_id is required");

        var q = db.PackingItems.Where(p => p.TripId == tripId);
        if (ctx.Arguments.TryGetPropertyValue("packed", out var pv) && pv is not null)
        {
            var packed = ctx.Arguments.GetBool("packed");
            q = q.Where(p => p.Packed == packed);
        }

        var rows = await q.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No packing items for this trip.");

        var sb = new StringBuilder($"{rows.Count} packing item(s):\n");
        foreach (var p in rows) sb.AppendLine(PackingFormat.Line(p));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class CheckPackingItemTool(TravelAgentDbContext db) : ITool
{
    public string Name => "check_packing_item";
    public string Description => "Toggle whether a packing item has been packed. Not destructive.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Packing item id (GUID)", required: true)
        .Bool("packed", "New packed state", required: true)
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid packing item id is required");
        var p = await db.PackingItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return ToolExecutionResult.Error("packing item not found");
        p.Packed = ctx.Arguments.GetBool("packed");
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Updated packing item.\n{PackingFormat.Line(p)}");
    }
}

public class DeletePackingItemTool(TravelAgentDbContext db) : ITool
{
    public string Name => "delete_packing_item";
    public string Description => "Permanently delete a packing item. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Packing item id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid packing item id is required");
        var p = await db.PackingItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return ToolExecutionResult.Error("packing item not found");
        db.PackingItems.Remove(p);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted packing item: {p.Name}");
    }
}
