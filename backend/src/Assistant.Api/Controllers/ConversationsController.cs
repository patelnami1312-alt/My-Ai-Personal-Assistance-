using Assistant.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController(AssistantDbContext db) : ControllerBase
{
    private static Guid UserId => AssistantDbContext.DefaultUserId;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await db.Conversations
            .Where(c => c.UserId == UserId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new { c.Id, c.Title, c.CreatedAt, c.UpdatedAt, MessageCount = c.Messages.Count })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var convo = await db.Conversations
            .Where(c => c.Id == id && c.UserId == UserId)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.CreatedAt,
                c.UpdatedAt,
                Messages = c.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new { m.Id, Role = m.Role.ToString(), m.Content, m.ToolTrace, m.CreatedAt })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);
        return convo is null ? NotFound() : Ok(convo);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var convo = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId, ct);
        if (convo is null) return NotFound();
        db.Conversations.Remove(convo);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
