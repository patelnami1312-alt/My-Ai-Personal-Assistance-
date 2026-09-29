using TravelAgent.Core.Dtos;
using TravelAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TravelAgent.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(TravelAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        var totalTrips = await db.Trips.CountAsync(ct);

        var upcoming = await db.Trips
            .Where(t => t.StartDate >= today)
            .OrderBy(t => t.StartDate)
            .FirstOrDefaultAsync(ct);

        UpcomingTripRow? upcomingRow = upcoming is null
            ? null
            : new UpcomingTripRow(upcoming.Id, upcoming.Destination, upcoming.StartDate, upcoming.EndDate);

        PackingProgressRow? packingProgress = null;
        if (upcoming is not null)
        {
            var items = await db.PackingItems.Where(p => p.TripId == upcoming.Id).ToListAsync(ct);
            if (items.Count > 0)
                packingProgress = new PackingProgressRow(items.Count(p => p.Packed), items.Count);
        }

        var summary = new DashboardSummary(totalTrips, upcomingRow, packingProgress);
        return Ok(summary);
    }
}
