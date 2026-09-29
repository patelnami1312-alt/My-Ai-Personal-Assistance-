using LearningAgent.Core.Dtos;
using LearningAgent.Core.Entities;
using LearningAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningAgent.Api.Controllers;

[ApiController]
[Route("api/study-sessions")]
public class StudySessionsController(LearningAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.StudySessions.OrderByDescending(s => s.Date).Take(200).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StudySessionInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Topic)) return BadRequest(new { error = "topic required" });
        if (input.DurationMinutes <= 0) return BadRequest(new { error = "durationMinutes must be positive" });
        var s = new StudySession
        {
            Topic = input.Topic.Trim(),
            DurationMinutes = input.DurationMinutes,
            CourseId = input.CourseId,
            Notes = input.Notes,
            Date = input.Date?.ToUniversalTime() ?? DateTime.UtcNow,
        };
        db.StudySessions.Add(s);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, s);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var s = await db.StudySessions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();
        db.StudySessions.Remove(s);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
