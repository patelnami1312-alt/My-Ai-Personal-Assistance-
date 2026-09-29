using FitnessAgent.Core.Dtos;
using FitnessAgent.Core.Entities;
using FitnessAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessAgent.Api.Controllers;

[ApiController]
[Route("api/bodyweight")]
public class BodyWeightController(FitnessAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.BodyWeights.OrderByDescending(w => w.Date).Take(200).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BodyWeightInput input, CancellationToken ct)
    {
        if (input.WeightKg <= 0) return BadRequest(new { error = "weightKg required" });
        var e = new BodyWeightEntry
        {
            WeightKg = input.WeightKg,
            Notes = input.Notes,
            Date = input.Date?.ToUniversalTime() ?? DateTime.UtcNow,
        };
        db.BodyWeights.Add(e);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, e);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var e = await db.BodyWeights.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();
        db.BodyWeights.Remove(e);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
