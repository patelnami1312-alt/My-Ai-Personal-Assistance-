using LearningAgent.Core.Dtos;
using LearningAgent.Core.Entities;
using LearningAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningAgent.Api.Controllers;

[ApiController]
[Route("api/goals")]
public class GoalsController(LearningAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Goals.OrderByDescending(g => g.CreatedAt).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GoalInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Description)) return BadRequest(new { error = "description required" });
        var g = new LearningGoal
        {
            Description = input.Description.Trim(),
            TargetDate = input.TargetDate,
            Status = Enum.TryParse<LearningGoalStatus>(input.Status, true, out var s) ? s : LearningGoalStatus.Active,
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
        g.TargetDate = input.TargetDate;
        if (Enum.TryParse<LearningGoalStatus>(input.Status, true, out var s)) g.Status = s;
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
