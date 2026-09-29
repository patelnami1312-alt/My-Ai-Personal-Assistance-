using FitnessAgent.Core.Dtos;
using FitnessAgent.Core.Entities;
using FitnessAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessAgent.Api.Controllers;

[ApiController]
[Route("api/workouts")]
public class WorkoutsController(FitnessAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Workouts.OrderByDescending(w => w.Date).Take(200).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkoutInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Exercise)) return BadRequest(new { error = "exercise required" });
        var w = new WorkoutEntry
        {
            Exercise = input.Exercise.Trim(),
            Sets = input.Sets,
            Reps = input.Reps,
            WeightKg = input.WeightKg,
            DurationMinutes = input.DurationMinutes,
            Notes = input.Notes,
            Date = input.Date?.ToUniversalTime() ?? DateTime.UtcNow,
        };
        db.Workouts.Add(w);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, w);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var w = await db.Workouts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (w is null) return NotFound();
        db.Workouts.Remove(w);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
