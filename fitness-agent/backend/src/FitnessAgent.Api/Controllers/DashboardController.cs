using FitnessAgent.Core.Dtos;
using FitnessAgent.Core.Entities;
using FitnessAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessAgent.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(FitnessAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var monthAgo = DateTime.UtcNow.AddDays(-30);

        var workoutsThisWeek = await db.Workouts.CountAsync(w => w.Date >= weekAgo, ct);

        var latest = await db.BodyWeights.OrderByDescending(w => w.Date).FirstOrDefaultAsync(ct);
        var monthOld = await db.BodyWeights
            .Where(w => w.Date <= monthAgo)
            .OrderByDescending(w => w.Date)
            .FirstOrDefaultAsync(ct);

        var activeGoals = await db.Goals.CountAsync(g => g.Status == GoalStatus.Active, ct);

        var recent = await db.Workouts
            .OrderByDescending(w => w.Date)
            .Take(5)
            .Select(w => new WorkoutSummaryRow(w.Id, w.Date, w.Exercise, w.Sets, w.Reps, w.WeightKg))
            .ToListAsync(ct);

        var summary = new DashboardSummary(
            workoutsThisWeek,
            latest?.WeightKg,
            latest is not null && monthOld is not null ? latest.WeightKg - monthOld.WeightKg : null,
            activeGoals,
            recent);

        return Ok(summary);
    }
}
