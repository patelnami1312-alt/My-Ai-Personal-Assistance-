using FinanceAgent.Core.Dtos;
using FinanceAgent.Core.Entities;
using FinanceAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(FinanceAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Transactions.OrderByDescending(t => t.Date).Take(200).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransactionInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Description)) return BadRequest(new { error = "description required" });
        if (!Enum.TryParse<TransactionType>(input.Type, true, out var type))
            return BadRequest(new { error = "type must be Income or Expense" });

        var t = new Transaction
        {
            Description = input.Description.Trim(),
            Amount = input.Amount,
            Type = type,
            Category = input.Category?.Trim() ?? "",
            Notes = input.Notes,
            Date = input.Date?.ToUniversalTime() ?? DateTime.UtcNow,
        };
        db.Transactions.Add(t);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, t);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var t = await db.Transactions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        db.Transactions.Remove(t);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
