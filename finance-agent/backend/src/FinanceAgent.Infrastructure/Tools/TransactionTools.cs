using System.Text;
using System.Text.Json.Nodes;
using FinanceAgent.Core.Abstractions;
using FinanceAgent.Core.Entities;
using FinanceAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Infrastructure.Tools;

internal static class TransactionFormat
{
    public static string Line(Transaction t) =>
        $"- {t.Date:yyyy-MM-dd} [{t.Type}] {t.Description} - {t.Amount:0.00} ({t.Category}) (id={t.Id})" +
        (string.IsNullOrWhiteSpace(t.Notes) ? "" : $"\n    {t.Notes}");
}

public class LogTransactionTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "log_transaction";
    public string Description => "Record an income or expense transaction.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("description", "Short description of the transaction", required: true)
        .Num("amount", "Amount, always positive", required: true)
        .Str("type", "Transaction type", required: true, enumValues: ["Income", "Expense"])
        .Str("category", "Category, e.g. 'Groceries', 'Salary', 'Rent'", required: true)
        .Str("date", "ISO-8601 date/time. Defaults to now.")
        .Str("notes", "Optional notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var description = ctx.Arguments.GetString("description");
        if (string.IsNullOrWhiteSpace(description)) return ToolExecutionResult.Error("description is required");
        var amount = ctx.Arguments.GetDecimal("amount");
        if (amount is null || amount <= 0) return ToolExecutionResult.Error("a positive amount is required");
        var category = ctx.Arguments.GetString("category");
        if (string.IsNullOrWhiteSpace(category)) return ToolExecutionResult.Error("category is required");
        if (!Enum.TryParse<TransactionType>(ctx.Arguments.GetString("type"), true, out var type))
            return ToolExecutionResult.Error("type must be Income or Expense");

        var t = new Transaction
        {
            Description = description.Trim(),
            Amount = amount.Value,
            Type = type,
            Category = category.Trim(),
            Notes = ctx.Arguments.GetString("notes"),
            Date = ctx.Arguments.GetDateUtc("date") ?? DateTime.UtcNow,
        };
        db.Transactions.Add(t);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Logged transaction.\n{TransactionFormat.Line(t)}");
    }
}

public class GetTransactionsTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "get_transactions";
    public string Description => "List logged transactions, most recent first, optionally filtered.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("from", "Range start (ISO-8601 date)")
        .Str("to", "Range end (ISO-8601 date)")
        .Str("category", "Filter by category (partial match)")
        .Str("type", "Filter by type", enumValues: ["Income", "Expense"])
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Transactions.AsQueryable();
        if (ctx.Arguments.GetDateUtc("from") is { } from) q = q.Where(t => t.Date >= from);
        if (ctx.Arguments.GetDateUtc("to") is { } to) q = q.Where(t => t.Date <= to);
        if (ctx.Arguments.GetString("category") is { } cat && !string.IsNullOrWhiteSpace(cat))
            q = q.Where(t => EF.Functions.ILike(t.Category, $"%{cat}%"));
        if (Enum.TryParse<TransactionType>(ctx.Arguments.GetString("type"), true, out var type))
            q = q.Where(t => t.Type == type);

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 200);
        var rows = await q.OrderByDescending(t => t.Date).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching transactions.");

        var sb = new StringBuilder($"{rows.Count} transaction(s):\n");
        foreach (var t in rows) sb.AppendLine(TransactionFormat.Line(t));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class DeleteTransactionTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "delete_transaction";
    public string Description => "Permanently delete a transaction. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Transaction id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid transaction id is required");
        var t = await db.Transactions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return ToolExecutionResult.Error("transaction not found");
        db.Transactions.Remove(t);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted transaction: {t.Description} ({t.Amount:0.00})");
    }
}

public class GetSpendingSummaryTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "get_spending_summary";
    public string Description => "Summarize total income, expenses, net, and a category breakdown for a date range.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("from", "Range start (ISO-8601 date). Defaults to start of current month.")
        .Str("to", "Range end (ISO-8601 date). Defaults to now.")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var from = ctx.Arguments.GetDateUtc("from") ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = ctx.Arguments.GetDateUtc("to") ?? now;

        var rows = await db.Transactions
            .Where(t => t.Date >= from && t.Date <= to)
            .ToListAsync(ct);

        var income = rows.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = rows.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var sb = new StringBuilder();
        sb.AppendLine($"Summary {from:yyyy-MM-dd} to {to:yyyy-MM-dd}:");
        sb.AppendLine($"- Total income: {income:0.00}");
        sb.AppendLine($"- Total expense: {expense:0.00}");
        sb.AppendLine($"- Net: {income - expense:0.00}");

        var byCategory = rows
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(g => g.Total);

        sb.AppendLine("Expense breakdown by category:");
        var any = false;
        foreach (var g in byCategory)
        {
            any = true;
            sb.AppendLine($"  - {g.Category}: {g.Total:0.00}");
        }
        if (!any) sb.AppendLine("  (no expenses in this range)");

        return ToolExecutionResult.Ok(sb.ToString());
    }
}
