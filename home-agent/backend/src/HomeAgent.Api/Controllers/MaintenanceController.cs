using HomeAgent.Core.Dtos;
using HomeAgent.Core.Entities;
using HomeAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeAgent.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
public class MaintenanceController(HomeAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.MaintenanceItems.OrderBy(m => m.NextDue == null).ThenBy(m => m.NextDue).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MaintenanceInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest(new { error = "title required" });
        var lastDone = input.LastDone?.ToUniversalTime();
        var m = new MaintenanceItem
        {
            Title = input.Title.Trim(),
            IntervalDays = input.IntervalDays,
            LastDone = lastDone,
            NextDue = (lastDone is { } ld && input.IntervalDays is { } iv) ? ld.AddDays(iv) : null,
            Notes = input.Notes,
        };
        db.MaintenanceItems.Add(m);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, m);
    }

    /// <summary>Full update, or pass only markDone=true to mark the item done today and recompute NextDue.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] MaintenanceInput input, CancellationToken ct)
    {
        var m = await db.MaintenanceItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(input.Title)) m.Title = input.Title.Trim();
        m.IntervalDays = input.IntervalDays;
        m.LastDone = input.LastDone?.ToUniversalTime();
        m.NextDue = (m.LastDone is { } ld && m.IntervalDays is { } iv) ? ld.AddDays(iv) : null;
        m.Notes = input.Notes;

        await db.SaveChangesAsync(ct);
        return Ok(m);
    }

    [HttpPut("{id:guid}/mark-done")]
    public async Task<IActionResult> MarkDone(Guid id, CancellationToken ct)
    {
        var m = await db.MaintenanceItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        m.LastDone = DateTime.UtcNow;
        m.NextDue = m.IntervalDays is { } iv ? m.LastDone.Value.AddDays(iv) : null;
        await db.SaveChangesAsync(ct);
        return Ok(m);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var m = await db.MaintenanceItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        db.MaintenanceItems.Remove(m);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
