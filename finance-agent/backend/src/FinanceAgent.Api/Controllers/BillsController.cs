using FinanceAgent.Core.Dtos;
using FinanceAgent.Core.Entities;
using FinanceAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Api.Controllers;

[ApiController]
[Route("api/bills")]
public class BillsController(FinanceAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Bills.OrderBy(b => b.DueDate).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BillInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest(new { error = "name required" });
        Enum.TryParse<BillFrequency>(input.Frequency, true, out var frequency);

        var b = new Bill
        {
            Name = input.Name.Trim(),
            Amount = input.Amount,
            DueDate = input.DueDate.ToUniversalTime(),
            Recurring = input.Recurring,
            Frequency = frequency,
            Notes = input.Notes,
        };
        db.Bills.Add(b);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, b);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BillInput input, CancellationToken ct)
    {
        var b = await db.Bills.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return NotFound();
        Enum.TryParse<BillFrequency>(input.Frequency, true, out var frequency);

        b.Name = input.Name.Trim();
        b.Amount = input.Amount;
        b.DueDate = input.DueDate.ToUniversalTime();
        b.Recurring = input.Recurring;
        b.Frequency = frequency;
        b.Notes = input.Notes;
        b.Paid = input.Paid;
        await db.SaveChangesAsync(ct);
        return Ok(b);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var b = await db.Bills.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return NotFound();
        db.Bills.Remove(b);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
