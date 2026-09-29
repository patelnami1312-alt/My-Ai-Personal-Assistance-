using TravelAgent.Core.Dtos;
using TravelAgent.Core.Entities;
using TravelAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TravelAgent.Api.Controllers;

[ApiController]
[Route("api/itinerary")]
public class ItineraryController(TravelAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid tripId, CancellationToken ct) =>
        Ok(await db.ItineraryItems.Where(i => i.TripId == tripId).OrderBy(i => i.StartsAt).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ItineraryItemInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest(new { error = "title required" });
        if (!Enum.TryParse<ItineraryItemType>(input.Type, true, out var type))
            return BadRequest(new { error = "invalid type" });
        var tripExists = await db.Trips.AnyAsync(t => t.Id == input.TripId, ct);
        if (!tripExists) return BadRequest(new { error = "trip not found" });

        var item = new ItineraryItem
        {
            TripId = input.TripId,
            Title = input.Title.Trim(),
            Type = type,
            StartsAt = input.StartsAt.ToUniversalTime(),
            EndsAt = input.EndsAt?.ToUniversalTime(),
            Location = input.Location,
            Notes = input.Notes,
        };
        db.ItineraryItems.Add(item);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { tripId = item.TripId }, item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await db.ItineraryItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        db.ItineraryItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
