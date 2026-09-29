using ShoppingAgent.Core.Dtos;
using ShoppingAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ShoppingAgent.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(ShoppingAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var activeItemsCount = await db.Items.CountAsync(i => !i.Purchased, ct);
        var purchasedThisWeekCount = await db.Items.CountAsync(i => i.Purchased && i.PurchasedAt >= weekAgo, ct);
        var estimatedTotal = await db.Items.Where(i => !i.Purchased).SumAsync(i => i.EstimatedPrice ?? 0, ct);

        var recentItems = await db.Items
            .OrderByDescending(i => i.CreatedAt)
            .Take(8)
            .Select(i => new RecentItemRow(i.Id, i.Name, i.Quantity, i.Category, i.Purchased))
            .ToListAsync(ct);

        var summary = new DashboardSummary(activeItemsCount, purchasedThisWeekCount, estimatedTotal, recentItems);
        return Ok(summary);
    }
}
