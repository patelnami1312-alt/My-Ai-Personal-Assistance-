using HomeAgent.Core.Dtos;
using HomeAgent.Core.Entities;
using HomeAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeAgent.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(HomeAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var soonCutoff = DateTime.UtcNow.AddDays(30);

        var choresDueToday = await db.Chores.CountAsync(
            c => c.Status == ChoreStatus.Todo && c.DueDate != null && c.DueDate >= today && c.DueDate < tomorrow, ct);

        var overdueChores = await db.Chores.CountAsync(
            c => c.Status == ChoreStatus.Todo && c.DueDate != null && c.DueDate < today, ct);

        var lowInventory = await db.InventoryItems.CountAsync(i => i.Quantity <= i.LowThreshold, ct);

        var upcomingMaintenance = await db.MaintenanceItems
            .Where(m => m.NextDue != null && m.NextDue <= soonCutoff)
            .OrderBy(m => m.NextDue)
            .Take(5)
            .Select(m => new UpcomingMaintenanceRow(m.Id, m.Title, m.NextDue))
            .ToListAsync(ct);

        var recentChores = await db.Chores
            .Where(c => c.Status == ChoreStatus.Todo)
            .OrderBy(c => c.DueDate == null).ThenBy(c => c.DueDate)
            .Take(5)
            .Select(c => new RecentChoreRow(c.Id, c.Title, c.Status.ToString(), c.DueDate))
            .ToListAsync(ct);

        var summary = new DashboardSummary(
            choresDueToday,
            overdueChores,
            lowInventory,
            upcomingMaintenance,
            recentChores);

        return Ok(summary);
    }
}
