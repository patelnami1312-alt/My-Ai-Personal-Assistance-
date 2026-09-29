using System.Text;
using System.Text.Json.Nodes;
using HomeAgent.Core.Abstractions;
using HomeAgent.Core.Entities;
using HomeAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeAgent.Infrastructure.Tools;

internal static class InventoryFormat
{
    public static string Line(InventoryItem i) =>
        $"- {i.Name} (id={i.Id}): {i.Quantity}" +
        (i.Category is { } cat && !string.IsNullOrWhiteSpace(cat) ? $" - {cat}" : "") +
        (i.Quantity <= i.LowThreshold ? " [LOW]" : "") +
        (string.IsNullOrWhiteSpace(i.Notes) ? "" : $"\n    {i.Notes}");
}

public class AddInventoryItemTool(HomeAgentDbContext db) : ITool
{
    public string Name => "add_inventory_item";
    public string Description => "Add a household inventory / supplies item.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("name", "Item name, e.g. 'Paper towels'", required: true)
        .Str("category", "Category, e.g. 'Cleaning', 'Pantry'")
        .Int("quantity", "Current quantity", required: true)
        .Int("low_threshold", "Quantity at or below which this is considered low")
        .Str("notes", "Optional notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var name = ctx.Arguments.GetString("name");
        if (string.IsNullOrWhiteSpace(name)) return ToolExecutionResult.Error("name is required");
        var quantity = ctx.Arguments.GetInt("quantity");
        if (quantity is null) return ToolExecutionResult.Error("quantity is required");

        var i = new InventoryItem
        {
            Name = name.Trim(),
            Category = ctx.Arguments.GetString("category"),
            Quantity = quantity.Value,
            LowThreshold = ctx.Arguments.GetInt("low_threshold") ?? 1,
            Notes = ctx.Arguments.GetString("notes"),
        };
        db.InventoryItems.Add(i);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Added inventory item.\n{InventoryFormat.Line(i)}");
    }
}

public class GetInventoryTool(HomeAgentDbContext db) : ITool
{
    public string Name => "get_inventory";
    public string Description => "List household inventory items, optionally only low-stock ones.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Bool("low_only", "Only items at or below their low threshold")
        .Str("category", "Filter by category (partial match)")
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.InventoryItems.AsQueryable();
        if (ctx.Arguments.GetBool("low_only"))
            q = q.Where(i => i.Quantity <= i.LowThreshold);
        if (ctx.Arguments.GetString("category") is { } cat && !string.IsNullOrWhiteSpace(cat))
            q = q.Where(i => i.Category != null && EF.Functions.ILike(i.Category, $"%{cat}%"));

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 100);
        var rows = await q.OrderBy(i => i.Name).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching inventory items.");

        var sb = new StringBuilder($"{rows.Count} inventory item(s):\n");
        foreach (var i in rows) sb.AppendLine(InventoryFormat.Line(i));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class UpdateInventoryQuantityTool(HomeAgentDbContext db) : ITool
{
    public string Name => "update_inventory_quantity";
    public string Description => "Set the quantity of an inventory item directly, e.g. 'we're down to 2 rolls of paper towels'.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Inventory item id (GUID)", required: true)
        .Int("quantity", "New quantity", required: true)
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid inventory item id is required");
        var quantity = ctx.Arguments.GetInt("quantity");
        if (quantity is null) return ToolExecutionResult.Error("quantity is required");

        var i = await db.InventoryItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i is null) return ToolExecutionResult.Error("inventory item not found");

        i.Quantity = quantity.Value;
        i.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Updated {i.Name} to quantity {i.Quantity}." + (i.Quantity <= i.LowThreshold ? " (low stock)" : ""));
    }
}

public class DeleteInventoryItemTool(HomeAgentDbContext db) : ITool
{
    public string Name => "delete_inventory_item";
    public string Description => "Permanently delete an inventory item. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Inventory item id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid inventory item id is required");
        var i = await db.InventoryItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i is null) return ToolExecutionResult.Error("inventory item not found");
        db.InventoryItems.Remove(i);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted inventory item: {i.Name}");
    }
}
