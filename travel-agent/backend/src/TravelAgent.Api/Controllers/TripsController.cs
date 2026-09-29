using TravelAgent.Core.Dtos;
using TravelAgent.Core.Entities;
using TravelAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TravelAgent.Api.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController(TravelAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Trips.OrderBy(t => t.StartDate).ToListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var t = await db.Trips.FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TripInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Destination)) return BadRequest(new { error = "destination required" });
        var status = TripStatus.Planned;
        if (!string.IsNullOrWhiteSpace(input.Status) && !Enum.TryParse(input.Status, true, out status))
            return BadRequest(new { error = "invalid status" });

        var t = new Trip
        {
            Destination = input.Destination.Trim(),
            StartDate = input.StartDate.ToUniversalTime(),
            EndDate = input.EndDate.ToUniversalTime(),
            Status = status,
            Notes = input.Notes,
        };
        db.Trips.Add(t);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = t.Id }, t);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TripInput input, CancellationToken ct)
    {
        var t = await db.Trips.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Destination)) return BadRequest(new { error = "destination required" });
        var status = t.Status;
        if (!string.IsNullOrWhiteSpace(input.Status) && !Enum.TryParse(input.Status, true, out status))
            return BadRequest(new { error = "invalid status" });

        t.Destination = input.Destination.Trim();
        t.StartDate = input.StartDate.ToUniversalTime();
        t.EndDate = input.EndDate.ToUniversalTime();
        t.Status = status;
        t.Notes = input.Notes;
        await db.SaveChangesAsync(ct);
        return Ok(t);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var t = await db.Trips.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        db.Trips.Remove(t);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
