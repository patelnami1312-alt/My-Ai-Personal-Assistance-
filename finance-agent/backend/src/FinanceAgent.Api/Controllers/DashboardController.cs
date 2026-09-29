using FinanceAgent.Core.Dtos;
using FinanceAgent.Core.Entities;
using FinanceAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(FinanceAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var twoWeeksOut = now.AddDays(14);

        var monthTransactions = await db.Transactions
            .Where(t => t.Date >= monthStart)
            .ToListAsync(ct);
        var totalIncome = monthTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = monthTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var totalAccountBalance = await db.Accounts.SumAsync(a => a.Balance, ct);

        var upcomingBills = await db.Bills
            .Where(b => !b.Paid && b.DueDate <= twoWeeksOut)
            .OrderBy(b => b.DueDate)
            .Take(5)
            .Select(b => new UpcomingBillRow(b.Id, b.Name, b.Amount, b.DueDate))
            .ToListAsync(ct);

        var budgets = await db.Budgets.OrderBy(b => b.Category).ToListAsync(ct);
        var spentByCategory = monthTransactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount), StringComparer.OrdinalIgnoreCase);
        var budgetStatus = budgets
            .Select(b => new BudgetStatusRow(b.Category, b.MonthlyLimit, spentByCategory.GetValueOrDefault(b.Category, 0m)))
            .ToList();

        var summary = new DashboardSummary(
            totalIncome,
            totalExpense,
            totalIncome - totalExpense,
            totalAccountBalance,
            upcomingBills,
            budgetStatus);

        return Ok(summary);
    }
}
