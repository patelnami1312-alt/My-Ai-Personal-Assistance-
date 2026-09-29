using FinanceAgent.Core.Dtos;
using FinanceAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Api.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController(FinanceAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await db.Conversations
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummary(c.Id, c.Title, c.CreatedAt, c.UpdatedAt, c.Messages.Count))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var convo = await db.Conversations
            .Where(c => c.Id == id)
            .Select(c => new ConversationDetail(
                c.Id, c.Title, c.CreatedAt, c.UpdatedAt,
                c.Messages.OrderBy(m => m.CreatedAt)
                    .Select(m => new ChatMessageDto(m.Id, m.Role.ToString(), m.Content, m.ToolTrace, m.CreatedAt))
                    .ToList()))
            .FirstOrDefaultAsync(ct);
        return convo is null ? NotFound() : Ok(convo);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var convo = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (convo is null) return NotFound();
        db.Conversations.Remove(convo);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
