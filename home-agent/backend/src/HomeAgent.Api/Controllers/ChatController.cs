using HomeAgent.Core.Abstractions;
using HomeAgent.Core.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HomeAgent.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(IOrchestrator orchestrator, ILogger<ChatController> log) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatTurnResult>> Post([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "message is required" });

        try
        {
            var result = await orchestrator.HandleAsync(request.ConversationId, request.Message.Trim(), ct);
            return Ok(result);
        }
        catch (System.ClientModel.ClientResultException ex) when (ex.Status is 401 or 403)
        {
            return StatusCode(502, new { error = "The LLM rejected the API key. Set a valid Llm:ApiKey in user-secrets." });
        }
        catch (System.ClientModel.ClientResultException ex)
        {
            log.LogError(ex, "LLM call failed ({Status})", ex.Status);
            return StatusCode(502, new { error = $"The LLM returned an error ({ex.Status}). Check the API logs." });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Chat turn failed");
            return StatusCode(502, new { error = "The assistant hit an error handling that. Check the API logs." });
        }
    }
}
