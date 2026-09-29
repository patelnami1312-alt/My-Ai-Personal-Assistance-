using FinanceAgent.Core.Dtos;
using FinanceAgent.Core.Entities;
using FinanceAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Api.Controllers;

[ApiController]
[Route("api/budgets")]
public class BudgetsController(FinanceAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Budgets.OrderBy(b => b.Category).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BudgetInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Category)) return BadRequest(new { error = "category required" });

        var b = new Budget
        {
            Category = input.Category.Trim(),
            MonthlyLimit = input.MonthlyLimit,
        };
        db.Budgets.Add(b);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, b);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var b = await db.Budgets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return NotFound();
        db.Budgets.Remove(b);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
