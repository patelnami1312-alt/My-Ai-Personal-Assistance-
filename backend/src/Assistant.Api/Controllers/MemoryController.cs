using Assistant.Core.Entities;
using Assistant.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Controllers;

[ApiController]
[Route("api/memories")]
public class MemoryController(AssistantDbContext db) : ControllerBase
{
    private static Guid UserId => AssistantDbContext.DefaultUserId;

    public record MemoryInput(string Content, string? Category, List<string>? Keywords);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, CancellationToken ct)
    {
        var query = db.Memories.Where(m => m.UserId == UserId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q}%";
            query = query.Where(m => EF.Functions.ILike(m.Content, like));
        }
        return Ok(await query.OrderByDescending(m => m.LastUsedAt).ToListAsync(ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MemoryInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Content)) return BadRequest(new { error = "content required" });
        var mem = new MemoryItem
        {
            UserId = UserId,
            Content = input.Content.Trim(),
            Category = Enum.TryParse<MemoryCategory>(input.Category, true, out var c) ? c : MemoryCategory.Fact,
            Keywords = input.Keywords ?? new(),
            Confidence = 1.0,
        };
        db.Memories.Add(mem);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, mem);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var mem = await db.Memories.FirstOrDefaultAsync(m => m.Id == id && m.UserId == UserId, ct);
        if (mem is null) return NotFound();
        db.Memories.Remove(mem);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/profile")]
public class ProfileController(AssistantDbContext db) : ControllerBase
{
    private static Guid UserId => AssistantDbContext.DefaultUserId;

    public record ProfileInput(string? Name, string? TimeZone, string? Language, List<string>? Preferences);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ProfileInput input, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(input.Name)) user.Name = input.Name.Trim();
        if (!string.IsNullOrWhiteSpace(input.TimeZone))
        {
            try { _ = TimeZoneInfo.FindSystemTimeZoneById(input.TimeZone); user.TimeZone = input.TimeZone; }
            catch { return BadRequest(new { error = $"unknown timezone: {input.TimeZone}" }); }
        }
        if (!string.IsNullOrWhiteSpace(input.Language)) user.Language = input.Language.Trim();
        if (input.Preferences is not null) user.Preferences = input.Preferences;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(user);
    }
}
