using System.Text;
using System.Text.Json.Nodes;
using FinanceAgent.Core.Abstractions;
using FinanceAgent.Core.Entities;
using FinanceAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Infrastructure.Tools;

public class SetBudgetTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "set_budget";
    public string Description => "Create or update the monthly budget limit for a category (upsert by category).";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("category", "Budget category, e.g. 'Groceries'", required: true)
        .Num("monthly_limit", "Monthly spending limit for this category", required: true)
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var category = ctx.Arguments.GetString("category");
        if (string.IsNullOrWhiteSpace(category)) return ToolExecutionResult.Error("category is required");
        var limit = ctx.Arguments.GetDecimal("monthly_limit");
        if (limit is null) return ToolExecutionResult.Error("monthly_limit is required");

        var trimmed = category.Trim();
        var b = await db.Budgets.FirstOrDefaultAsync(x => x.Category == trimmed, ct);
        if (b is null)
        {
            b = new Budget { Category = trimmed, MonthlyLimit = limit.Value };
            db.Budgets.Add(b);
        }
        else
        {
            b.MonthlyLimit = limit.Value;
        }
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Budget set: {b.Category} = {b.MonthlyLimit:0.00}/month (id={b.Id})");
    }
}

public class GetBudgetsTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "get_budgets";
    public string Description => "List budgets, each with the amount spent this calendar month in that category vs the limit.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object().Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var budgets = await db.Budgets.OrderBy(b => b.Category).ToListAsync(ct);
        if (budgets.Count == 0) return ToolExecutionResult.Ok("No budgets set.");

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var spentByCategory = await db.Transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date >= monthStart)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);
        var spentMap = spentByCategory.ToDictionary(x => x.Category, x => x.Total, StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder($"{budgets.Count} budget(s) for {now:MMMM yyyy}:\n");
        foreach (var b in budgets)
        {
            var spent = spentMap.GetValueOrDefault(b.Category, 0m);
            sb.AppendLine($"- {b.Category}: spent {spent:0.00} / limit {b.MonthlyLimit:0.00} (id={b.Id})" +
                          (spent > b.MonthlyLimit ? " OVER BUDGET" : ""));
        }
        return ToolExecutionResult.Ok(sb.ToString());
    }
}
