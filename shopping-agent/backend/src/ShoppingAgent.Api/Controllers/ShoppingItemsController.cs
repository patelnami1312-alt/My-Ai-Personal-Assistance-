using ShoppingAgent.Core.Dtos;
using ShoppingAgent.Core.Entities;
using ShoppingAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ShoppingAgent.Api.Controllers;

[ApiController]
[Route("api/items")]
public class ShoppingItemsController(ShoppingAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? purchased, CancellationToken ct)
    {
        var q = db.Items.AsQueryable();
        if (purchased is { } p) q = q.Where(i => i.Purchased == p);
        var items = await q.OrderByDescending(i => i.CreatedAt).Take(200).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ShoppingItemInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest(new { error = "name required" });
        var item = new ShoppingItem
        {
            Name = input.Name.Trim(),
            Quantity = input.Quantity ?? 1,
            Category = input.Category,
            Store = input.Store,
            EstimatedPrice = input.EstimatedPrice,
            Notes = input.Notes,
            Purchased = input.Purchased ?? false,
        };
        db.Items.Add(item);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ShoppingItemInput input, CancellationToken ct)
    {
        var item = await db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(input.Name)) item.Name = input.Name.Trim();
        if (input.Quantity is { } qty) item.Quantity = qty;
        if (input.Category is not null) item.Category = input.Category;
        if (input.Store is not null) item.Store = input.Store;
        if (input.EstimatedPrice is { } price) item.EstimatedPrice = price;
        if (input.Notes is not null) item.Notes = input.Notes;
        if (input.Purchased is { } purchased)
        {
            item.Purchased = purchased;
            item.PurchasedAt = purchased ? (item.PurchasedAt ?? DateTime.UtcNow) : null;
        }

        await db.SaveChangesAsync(ct);
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        db.Items.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
