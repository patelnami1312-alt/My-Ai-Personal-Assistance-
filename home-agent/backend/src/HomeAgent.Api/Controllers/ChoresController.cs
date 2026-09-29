using HomeAgent.Core.Dtos;
using HomeAgent.Core.Entities;
using HomeAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeAgent.Api.Controllers;

[ApiController]
[Route("api/chores")]
public class ChoresController(HomeAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Chores.OrderBy(c => c.Status).ThenBy(c => c.DueDate == null).ThenBy(c => c.DueDate).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChoreInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest(new { error = "title required" });
        var freq = Enum.TryParse<RecurrenceFrequency>(input.Frequency, true, out var f) ? f : RecurrenceFrequency.OneTime;
        var c = new Chore
        {
            Title = input.Title.Trim(),
            AssignedTo = input.AssignedTo,
            DueDate = input.DueDate?.ToUniversalTime(),
            Recurring = input.Recurring ?? freq != RecurrenceFrequency.OneTime,
            Frequency = freq,
            Notes = input.Notes,
        };
        db.Chores.Add(c);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, c);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ChoreInput input, CancellationToken ct)
    {
        var c = await db.Chores.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(input.Title)) c.Title = input.Title.Trim();
        c.AssignedTo = input.AssignedTo;
        c.DueDate = input.DueDate?.ToUniversalTime();
        if (Enum.TryParse<RecurrenceFrequency>(input.Frequency, true, out var f)) c.Frequency = f;
        if (input.Recurring is { } r) c.Recurring = r;
        c.Notes = input.Notes;

        if (Enum.TryParse<ChoreStatus>(input.Status, true, out var s))
        {
            c.Status = s;
            c.CompletedAt = s == ChoreStatus.Done ? (c.CompletedAt ?? DateTime.UtcNow) : null;
        }

        await db.SaveChangesAsync(ct);
        return Ok(c);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var c = await db.Chores.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        db.Chores.Remove(c);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
