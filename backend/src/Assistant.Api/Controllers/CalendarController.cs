using Assistant.Core.Entities;
using Assistant.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Controllers;

[ApiController]
[Route("api/events")]
public class CalendarController(AssistantDbContext db) : ControllerBase
{
    private static Guid UserId => AssistantDbContext.DefaultUserId;

    public record EventInput(string Title, string? Description, string? Location, DateTime StartsAt, DateTime EndsAt, bool AllDay);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var f = (from ?? DateTime.UtcNow.AddDays(-1)).ToUniversalTime();
        var t = (to ?? f.AddDays(30)).ToUniversalTime();
        var events = await db.Events
            .Where(e => e.UserId == UserId && e.StartsAt < t && e.EndsAt > f)
            .OrderBy(e => e.StartsAt).ToListAsync(ct);
        return Ok(events);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EventInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest(new { error = "title required" });
        var start = input.StartsAt.ToUniversalTime();
        var end = input.EndsAt.ToUniversalTime();
        if (end <= start) end = start.AddHours(1);
        var ev = new CalendarEvent
        {
            UserId = UserId,
            Title = input.Title.Trim(),
            Description = input.Description,
            Location = input.Location,
            StartsAt = start,
            EndsAt = end,
            AllDay = input.AllDay,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, ev);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] EventInput input, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id && e.UserId == UserId, ct);
        if (ev is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(input.Title)) ev.Title = input.Title.Trim();
        ev.Description = input.Description;
        ev.Location = input.Location;
        ev.StartsAt = input.StartsAt.ToUniversalTime();
        ev.EndsAt = input.EndsAt.ToUniversalTime();
        if (ev.EndsAt <= ev.StartsAt) ev.EndsAt = ev.StartsAt.AddHours(1);
        ev.AllDay = input.AllDay;
        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ev);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id && e.UserId == UserId, ct);
        if (ev is null) return NotFound();
        db.Events.Remove(ev);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
