using Assistant.Core.Entities;
using Assistant.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController(AssistantDbContext db) : ControllerBase
{
    private static Guid UserId => AssistantDbContext.DefaultUserId;

    public record TaskInput(string Title, string? Description, string? Priority, DateTime? DueAt, string? Status);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var q = db.Tasks.Where(t => t.UserId == UserId);
        if (Enum.TryParse<TaskState>(status, true, out var s)) q = q.Where(t => t.Status == s);
        var tasks = await q
            .OrderBy(t => t.Status).ThenBy(t => t.DueAt ?? DateTime.MaxValue).ThenByDescending(t => t.Priority)
            .ToListAsync(ct);
        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest(new { error = "title required" });
        var task = new TaskItem
        {
            UserId = UserId,
            Title = input.Title.Trim(),
            Description = input.Description,
            Priority = Enum.TryParse<TaskPriority>(input.Priority, true, out var p) ? p : TaskPriority.Medium,
            DueAt = input.DueAt?.ToUniversalTime(),
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, task);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaskInput input, CancellationToken ct)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId, ct);
        if (task is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(input.Title)) task.Title = input.Title.Trim();
        task.Description = input.Description;
        if (Enum.TryParse<TaskPriority>(input.Priority, true, out var p)) task.Priority = p;
        task.DueAt = input.DueAt?.ToUniversalTime();
        if (Enum.TryParse<TaskState>(input.Status, true, out var s))
        {
            task.Status = s;
            task.CompletedAt = s == TaskState.Done ? DateTime.UtcNow : null;
        }
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(task);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId, ct);
        if (task is null) return NotFound();
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
