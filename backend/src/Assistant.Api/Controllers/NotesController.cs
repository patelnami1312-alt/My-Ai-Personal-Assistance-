using Assistant.Core.Entities;
using Assistant.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Controllers;

[ApiController]
[Route("api/notes")]
public class NotesController(AssistantDbContext db) : ControllerBase
{
    private static Guid UserId => AssistantDbContext.DefaultUserId;

    public record NoteInput(string Title, string Content, List<string>? Tags);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, CancellationToken ct)
    {
        var query = db.Notes.Where(n => n.UserId == UserId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q}%";
            query = query.Where(n => EF.Functions.ILike(n.Title, like) || EF.Functions.ILike(n.Content, like));
        }
        return Ok(await query.OrderByDescending(n => n.UpdatedAt).ToListAsync(ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NoteInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Content))
            return BadRequest(new { error = "title and content required" });
        var note = new Note
        {
            UserId = UserId,
            Title = input.Title.Trim(),
            Content = input.Content.Trim(),
            Tags = input.Tags ?? new(),
        };
        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, note);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] NoteInput input, CancellationToken ct)
    {
        var note = await db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId, ct);
        if (note is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(input.Title)) note.Title = input.Title.Trim();
        if (!string.IsNullOrWhiteSpace(input.Content)) note.Content = input.Content.Trim();
        note.Tags = input.Tags ?? note.Tags;
        note.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(note);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var note = await db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId, ct);
        if (note is null) return NotFound();
        db.Notes.Remove(note);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
