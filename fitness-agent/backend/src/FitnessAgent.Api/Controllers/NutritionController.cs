using FitnessAgent.Core.Dtos;
using FitnessAgent.Core.Entities;
using FitnessAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessAgent.Api.Controllers;

[ApiController]
[Route("api/nutrition")]
public class NutritionController(FitnessAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Nutrition.OrderByDescending(n => n.Date).Take(200).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NutritionInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Description)) return BadRequest(new { error = "description required" });
        var n = new NutritionEntry
        {
            Description = input.Description.Trim(),
            Calories = input.Calories,
            ProteinG = input.ProteinG,
            CarbsG = input.CarbsG,
            FatG = input.FatG,
            Date = input.Date?.ToUniversalTime() ?? DateTime.UtcNow,
        };
        db.Nutrition.Add(n);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, n);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var n = await db.Nutrition.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null) return NotFound();
        db.Nutrition.Remove(n);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
