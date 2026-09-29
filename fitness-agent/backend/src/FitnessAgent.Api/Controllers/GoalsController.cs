using FitnessAgent.Core.Dtos;
using FitnessAgent.Core.Entities;
using FitnessAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessAgent.Api.Controllers;

[ApiController]
[Route("api/goals")]
public class GoalsController(FitnessAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Goals.OrderBy(g => g.Status).ThenByDescending(g => g.CreatedAt).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GoalInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Description)) return BadRequest(new { error = "description required" });
        var g = new FitnessGoal
        {
            Description = input.Description.Trim(),
            Type = Enum.TryParse<GoalType>(input.Type, true, out var t) ? t : GoalType.Custom,
            TargetValue = input.TargetValue,
            TargetDate = input.TargetDate?.ToUniversalTime(),
        };
        db.Goals.Add(g);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, g);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] GoalInput input, CancellationToken ct)
    {
        var g = await db.Goals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(input.Description)) g.Description = input.Description.Trim();
        if (Enum.TryParse<GoalType>(input.Type, true, out var t)) g.Type = t;
        g.TargetValue = input.TargetValue;
        g.TargetDate = input.TargetDate?.ToUniversalTime();
        if (Enum.TryParse<GoalStatus>(input.Status, true, out var s)) g.Status = s;
        await db.SaveChangesAsync(ct);
        return Ok(g);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var g = await db.Goals.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return NotFound();
        db.Goals.Remove(g);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
