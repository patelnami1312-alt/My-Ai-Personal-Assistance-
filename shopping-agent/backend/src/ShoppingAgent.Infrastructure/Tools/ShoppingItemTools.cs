using System.Text;
using System.Text.Json.Nodes;
using ShoppingAgent.Core.Abstractions;
using ShoppingAgent.Core.Entities;
using ShoppingAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ShoppingAgent.Infrastructure.Tools;

internal static class ShoppingItemFormat
{
    public static string Line(ShoppingItem i) =>
        $"- {i.Name} x{i.Quantity} (id={i.Id})" +
        (string.IsNullOrWhiteSpace(i.Category) ? "" : $" [{i.Category}]") +
        (string.IsNullOrWhiteSpace(i.Store) ? "" : $" @ {i.Store}") +
        (i.EstimatedPrice is { } p ? $" - est. {p:C}" : "") +
        (i.Purchased ? " (purchased)" : "") +
        (string.IsNullOrWhiteSpace(i.Notes) ? "" : $"\n    {i.Notes}");
}

public class AddItemTool(ShoppingAgentDbContext db) : ITool
{
    public string Name => "add_item";
    public string Description => "Add an item to the shopping list.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("name", "Item name, e.g. 'Milk'", required: true)
        .Int("quantity", "How many. Defaults to 1.")
        .Str("category", "Category, e.g. 'Dairy' or 'Produce'")
        .Str("store", "Store to buy it at")
        .Num("estimated_price", "Estimated price for this item")
        .Str("notes", "Optional notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var name = ctx.Arguments.GetString("name");
        if (string.IsNullOrWhiteSpace(name)) return ToolExecutionResult.Error("name is required");

        var item = new ShoppingItem
        {
            Name = name.Trim(),
            Quantity = ctx.Arguments.GetInt("quantity") ?? 1,
            Category = ctx.Arguments.GetString("category"),
            Store = ctx.Arguments.GetString("store"),
            EstimatedPrice = ctx.Arguments.GetDecimal("estimated_price"),
            Notes = ctx.Arguments.GetString("notes"),
            Purchased = false,
        };
        db.Items.Add(item);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Added to shopping list.\n{ShoppingItemFormat.Line(item)}");
    }
}

public class GetShoppingListTool(ShoppingAgentDbContext db) : ITool
{
    public string Name => "get_shopping_list";
    public string Description => "List items on the shopping list. By default shows only unpurchased items.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Bool("purchased", "Filter by purchased state. If omitted, shows unpurchased items only.")
        .Str("category", "Filter by category (partial match)")
        .Str("store", "Filter by store (partial match)")
        .Int("limit", "Max rows (default 50)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Items.AsQueryable();
        var purchased = ctx.Arguments.GetBoolOrNull("purchased");
        q = q.Where(i => i.Purchased == (purchased ?? false));

        if (ctx.Arguments.GetString("category") is { } cat && !string.IsNullOrWhiteSpace(cat))
            q = q.Where(i => i.Category != null && EF.Functions.ILike(i.Category, $"%{cat}%"));
        if (ctx.Arguments.GetString("store") is { } store && !string.IsNullOrWhiteSpace(store))
            q = q.Where(i => i.Store != null && EF.Functions.ILike(i.Store, $"%{store}%"));

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 50, 1, 200);
        var rows = await q.OrderByDescending(i => i.CreatedAt).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching items.");

        var sb = new StringBuilder($"{rows.Count} item(s):\n");
        foreach (var i in rows) sb.AppendLine(ShoppingItemFormat.Line(i));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class MarkPurchasedTool(ShoppingAgentDbContext db) : ITool
{
    public string Name => "mark_purchased";
    public string Description => "Mark a shopping list item as purchased.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Item id (GUID)", required: true)
        .Num("actual_price", "The actual price paid, if different from the estimate")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid item id is required");
        var item = await db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ToolExecutionResult.Error("item not found");

        item.Purchased = true;
        item.PurchasedAt = DateTime.UtcNow;
        if (ctx.Arguments.GetDecimal("actual_price") is { } price) item.EstimatedPrice = price;
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Marked purchased.\n{ShoppingItemFormat.Line(item)}");
    }
}

public class UpdateItemTool(ShoppingAgentDbContext db) : ITool
{
    public string Name => "update_item";
    public string Description => "Update fields on an existing shopping list item.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Item id (GUID)", required: true)
        .Int("quantity", "New quantity")
        .Str("category", "New category")
        .Str("store", "New store")
        .Num("estimated_price", "New estimated price")
        .Str("notes", "New notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid item id is required");
        var item = await db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ToolExecutionResult.Error("item not found");

        if (ctx.Arguments.GetInt("quantity") is { } qty) item.Quantity = qty;
        if (ctx.Arguments.GetString("category") is { } cat) item.Category = cat;
        if (ctx.Arguments.GetString("store") is { } store) item.Store = store;
        if (ctx.Arguments.GetDecimal("estimated_price") is { } price) item.EstimatedPrice = price;
        if (ctx.Arguments.GetString("notes") is { } notes) item.Notes = notes;

        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Updated item.\n{ShoppingItemFormat.Line(item)}");
    }
}

public class DeleteItemTool(ShoppingAgentDbContext db) : ITool
{
    public string Name => "delete_item";
    public string Description => "Permanently delete a shopping list item. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Item id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid item id is required");
        var item = await db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ToolExecutionResult.Error("item not found");
        db.Items.Remove(item);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted item: {item.Name}");
    }
}

public class GetPurchaseHistoryTool(ShoppingAgentDbContext db) : ITool
{
    public string Name => "get_purchase_history";
    public string Description => "List previously purchased items, most recently purchased first.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("from", "Range start (ISO-8601 date), filters by purchase date")
        .Str("to", "Range end (ISO-8601 date), filters by purchase date")
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Items.Where(i => i.Purchased);
        if (ctx.Arguments.GetDateUtc("from") is { } from) q = q.Where(i => i.PurchasedAt >= from);
        if (ctx.Arguments.GetDateUtc("to") is { } to) q = q.Where(i => i.PurchasedAt <= to);

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 100);
        var rows = await q.OrderByDescending(i => i.PurchasedAt).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No purchase history.");

        var sb = new StringBuilder($"{rows.Count} purchased item(s):\n");
        foreach (var i in rows) sb.AppendLine(ShoppingItemFormat.Line(i));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}
