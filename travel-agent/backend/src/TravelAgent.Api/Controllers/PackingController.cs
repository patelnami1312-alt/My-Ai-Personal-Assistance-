using TravelAgent.Core.Dtos;
using TravelAgent.Core.Entities;
using TravelAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TravelAgent.Api.Controllers;

[ApiController]
[Route("api/packing")]
public class PackingController(TravelAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid tripId, CancellationToken ct) =>
        Ok(await db.PackingItems.Where(p => p.TripId == tripId).OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PackingItemInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest(new { error = "name required" });
        var tripExists = await db.Trips.AnyAsync(t => t.Id == input.TripId, ct);
        if (!tripExists) return BadRequest(new { error = "trip not found" });

        var item = new PackingItem
        {
            TripId = input.TripId,
            Name = input.Name.Trim(),
            Category = input.Category,
            Packed = input.Packed ?? false,
        };
        db.PackingItems.Add(item);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { tripId = item.TripId }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PackingItemInput input, CancellationToken ct)
    {
        var item = await db.PackingItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(input.Name)) item.Name = input.Name.Trim();
        item.Category = input.Category;
        if (input.Packed is { } packed) item.Packed = packed;
        await db.SaveChangesAsync(ct);
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await db.PackingItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        db.PackingItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
