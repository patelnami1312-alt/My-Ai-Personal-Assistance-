using System.Text;
using System.Text.Json.Nodes;
using FinanceAgent.Core.Abstractions;
using FinanceAgent.Core.Entities;
using FinanceAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Infrastructure.Tools;

internal static class AccountFormat
{
    public static string Line(FinanceAccount a) =>
        $"- {a.Name} [{a.Type}]: {a.Balance:0.00} (id={a.Id})" +
        (string.IsNullOrWhiteSpace(a.Notes) ? "" : $" - {a.Notes}");
}

public class AddAccountTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "add_account";
    public string Description => "Add a new financial account (checking, savings, credit card, cash, or investment).";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("name", "Account name, e.g. 'Chase Checking'", required: true)
        .Str("type", "Account type", required: true, enumValues: ["Checking", "Savings", "Credit", "Cash", "Investment"])
        .Num("balance", "Current balance", required: true)
        .Str("notes", "Optional notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var name = ctx.Arguments.GetString("name");
        if (string.IsNullOrWhiteSpace(name)) return ToolExecutionResult.Error("name is required");
        if (!Enum.TryParse<AccountType>(ctx.Arguments.GetString("type"), true, out var type))
            return ToolExecutionResult.Error("type must be one of Checking, Savings, Credit, Cash, Investment");
        var balance = ctx.Arguments.GetDecimal("balance");
        if (balance is null) return ToolExecutionResult.Error("balance is required");

        var a = new FinanceAccount
        {
            Name = name.Trim(),
            Type = type,
            Balance = balance.Value,
            Notes = ctx.Arguments.GetString("notes"),
        };
        db.Accounts.Add(a);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Account added.\n{AccountFormat.Line(a)}");
    }
}

public class GetAccountsTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "get_accounts";
    public string Description => "List all financial accounts with balances and the total across all accounts.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object().Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var rows = await db.Accounts.OrderBy(a => a.Name).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No accounts yet.");

        var sb = new StringBuilder($"{rows.Count} account(s):\n");
        foreach (var a in rows) sb.AppendLine(AccountFormat.Line(a));
        sb.AppendLine($"Total balance: {rows.Sum(a => a.Balance):0.00}");
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class UpdateAccountBalanceTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "update_account_balance";
    public string Description => "Set an account's balance directly, e.g. after reconciling with the bank.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Account id (GUID)", required: true)
        .Num("balance", "New balance", required: true)
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid account id is required");
        var balance = ctx.Arguments.GetDecimal("balance");
        if (balance is null) return ToolExecutionResult.Error("balance is required");

        var a = await db.Accounts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return ToolExecutionResult.Error("account not found");
        a.Balance = balance.Value;
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Updated balance.\n{AccountFormat.Line(a)}");
    }
}
