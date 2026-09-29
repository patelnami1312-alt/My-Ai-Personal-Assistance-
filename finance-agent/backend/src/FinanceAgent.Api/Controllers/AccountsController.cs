using FinanceAgent.Core.Dtos;
using FinanceAgent.Core.Entities;
using FinanceAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController(FinanceAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Accounts.OrderBy(a => a.Name).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AccountInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest(new { error = "name required" });
        if (!Enum.TryParse<AccountType>(input.Type, true, out var type))
            return BadRequest(new { error = "invalid account type" });

        var a = new FinanceAccount
        {
            Name = input.Name.Trim(),
            Type = type,
            Balance = input.Balance,
            Notes = input.Notes,
        };
        db.Accounts.Add(a);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, a);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AccountInput input, CancellationToken ct)
    {
        var a = await db.Accounts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return NotFound();
        if (!Enum.TryParse<AccountType>(input.Type, true, out var type))
            return BadRequest(new { error = "invalid account type" });

        a.Name = input.Name.Trim();
        a.Type = type;
        a.Balance = input.Balance;
        a.Notes = input.Notes;
        await db.SaveChangesAsync(ct);
        return Ok(a);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var a = await db.Accounts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return NotFound();
        db.Accounts.Remove(a);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
