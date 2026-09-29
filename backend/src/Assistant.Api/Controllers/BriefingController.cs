using Assistant.Core.Abstractions;
using Assistant.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Assistant.Api.Controllers;

[ApiController]
[Route("api/briefing")]
public class BriefingController(IBriefingService briefing) : ControllerBase
{
    /// <summary>The proactive daily briefing: today's events, open/overdue tasks, and a spoken-style summary.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await briefing.BuildAsync(AssistantDbContext.DefaultUserId, ct));
}
