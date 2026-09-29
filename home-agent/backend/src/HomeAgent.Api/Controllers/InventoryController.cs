using HomeAgent.Core.Dtos;
using HomeAgent.Core.Entities;
using HomeAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeAgent.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController(HomeAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.InventoryItems.OrderBy(i => i.Name).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InventoryInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest(new { error = "name required" });
        var i = new InventoryItem
        {
            Name = input.Name.Trim(),
            Category = input.Category,
            Quantity = input.Quantity,
            LowThreshold = input.LowThreshold ?? 1,
            Notes = input.Notes,
        };
        db.InventoryItems.Add(i);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, i);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] InventoryInput input, CancellationToken ct)
    {
        var i = await db.InventoryItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(input.Name)) i.Name = input.Name.Trim();
        i.Category = input.Category;
        i.Quantity = input.Quantity;
        if (input.LowThreshold is { } lt) i.LowThreshold = lt;
        i.Notes = input.Notes;
        i.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(i);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var i = await db.InventoryItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i is null) return NotFound();
        db.InventoryItems.Remove(i);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
