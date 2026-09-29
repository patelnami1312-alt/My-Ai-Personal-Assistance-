using LearningAgent.Core.Dtos;
using LearningAgent.Core.Entities;
using LearningAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningAgent.Api.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(LearningAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Courses.OrderByDescending(c => c.StartedAt).Take(200).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CourseInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest(new { error = "title required" });
        var c = new Course
        {
            Title = input.Title.Trim(),
            Subject = input.Subject,
            Notes = input.Notes,
            Status = Enum.TryParse<CourseStatus>(input.Status, true, out var s) ? s : CourseStatus.Active,
            StartedAt = input.StartedAt?.ToUniversalTime() ?? DateTime.UtcNow,
        };
        db.Courses.Add(c);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, c);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CourseInput input, CancellationToken ct)
    {
        var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(input.Title)) c.Title = input.Title.Trim();
        c.Subject = input.Subject;
        c.Notes = input.Notes;
        if (Enum.TryParse<CourseStatus>(input.Status, true, out var s)) c.Status = s;
        await db.SaveChangesAsync(ct);
        return Ok(c);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        db.Courses.Remove(c);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
