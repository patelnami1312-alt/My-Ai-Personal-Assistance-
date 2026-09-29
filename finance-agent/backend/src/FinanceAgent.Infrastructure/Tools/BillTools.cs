using System.Text;
using System.Text.Json.Nodes;
using FinanceAgent.Core.Abstractions;
using FinanceAgent.Core.Entities;
using FinanceAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Infrastructure.Tools;

internal static class BillFormat
{
    public static string Line(Bill b) =>
        $"- {b.Name}: {b.Amount:0.00}, due {b.DueDate:yyyy-MM-dd}" +
        (b.Paid ? " [PAID]" : " [UNPAID]") +
        (b.Recurring ? $", recurring {b.Frequency}" : "") +
        $" (id={b.Id})" +
        (string.IsNullOrWhiteSpace(b.Notes) ? "" : $"\n    {b.Notes}");
}

public class AddBillTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "add_bill";
    public string Description => "Add a bill to track, optionally recurring.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("name", "Bill name, e.g. 'Electricity'", required: true)
        .Num("amount", "Amount due", required: true)
        .Str("due_date", "Due date (ISO-8601)", required: true)
        .Bool("recurring", "Whether this bill repeats")
        .Str("frequency", "Recurrence frequency", enumValues: ["OneTime", "Weekly", "Monthly", "Yearly"])
        .Str("notes", "Optional notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var name = ctx.Arguments.GetString("name");
        if (string.IsNullOrWhiteSpace(name)) return ToolExecutionResult.Error("name is required");
        var amount = ctx.Arguments.GetDecimal("amount");
        if (amount is null) return ToolExecutionResult.Error("amount is required");
        var dueDate = ctx.Arguments.GetDateUtc("due_date");
        if (dueDate is null) return ToolExecutionResult.Error("a valid due_date is required");

        var recurring = ctx.Arguments.GetBool("recurring");
        Enum.TryParse<BillFrequency>(ctx.Arguments.GetString("frequency"), true, out var frequency);
        if (recurring && frequency == BillFrequency.OneTime) frequency = BillFrequency.Monthly;

        var b = new Bill
        {
            Name = name.Trim(),
            Amount = amount.Value,
            DueDate = dueDate.Value,
            Recurring = recurring,
            Frequency = frequency,
            Notes = ctx.Arguments.GetString("notes"),
        };
        db.Bills.Add(b);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Bill added.\n{BillFormat.Line(b)}");
    }
}

public class GetBillsTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "get_bills";
    public string Description => "List bills, soonest due first.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Bool("unpaid_only", "Only show unpaid bills")
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Bills.AsQueryable();
        if (ctx.Arguments.GetBool("unpaid_only")) q = q.Where(b => !b.Paid);

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 100);
        var rows = await q.OrderBy(b => b.DueDate).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching bills.");

        var sb = new StringBuilder($"{rows.Count} bill(s):\n");
        foreach (var b in rows) sb.AppendLine(BillFormat.Line(b));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class MarkBillPaidTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "mark_bill_paid";
    public string Description => "Mark a bill as paid. If it is recurring, advances the due date to the next cycle and resets it to unpaid.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Bill id (GUID)", required: true)
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid bill id is required");
        var b = await db.Bills.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return ToolExecutionResult.Error("bill not found");

        if (b.Recurring)
        {
            b.DueDate = b.Frequency switch
            {
                BillFrequency.Weekly => b.DueDate.AddDays(7),
                BillFrequency.Monthly => b.DueDate.AddMonths(1),
                BillFrequency.Yearly => b.DueDate.AddYears(1),
                _ => b.DueDate,
            };
            b.Paid = false;
            await db.SaveChangesAsync(ct);
            return ToolExecutionResult.Ok($"Marked paid. Recurring bill advanced to next due date.\n{BillFormat.Line(b)}");
        }

        b.Paid = true;
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Marked paid.\n{BillFormat.Line(b)}");
    }
}

public class DeleteBillTool(FinanceAgentDbContext db) : ITool
{
    public string Name => "delete_bill";
    public string Description => "Permanently delete a bill. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Bill id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid bill id is required");
        var b = await db.Bills.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return ToolExecutionResult.Error("bill not found");
        db.Bills.Remove(b);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted bill: {b.Name}");
    }
}
