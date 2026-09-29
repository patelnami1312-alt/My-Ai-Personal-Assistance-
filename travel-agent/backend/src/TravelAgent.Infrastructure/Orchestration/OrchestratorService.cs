using System.Text;
using System.Text.Json;
using TravelAgent.Core.Abstractions;
using TravelAgent.Core.Dtos;
using TravelAgent.Core.Entities;
using TravelAgent.Core.Llm;
using TravelAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TravelAgent.Infrastructure.Orchestration;

/// <summary>
/// The brain. Takes a user message, runs a bounded tool-use loop against the LLM,
/// and persists the exchange.
/// </summary>
public class OrchestratorService(
    TravelAgentDbContext db,
    ILlmProvider llm,
    IToolRegistry tools,
    ILogger<OrchestratorService> log) : IOrchestrator
{
    private const int MaxToolIterations = 6;
    private const int HistoryTurns = 16;

    public async Task<ChatTurnResult> HandleAsync(Guid? conversationId, string userMessage, CancellationToken ct = default)
    {
        var conversation = conversationId is { } cid
            ? await db.Conversations.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == cid, ct)
            : null;

        if (conversation is null)
        {
            conversation = new Conversation { Title = Truncate(userMessage, 60) };
            db.Conversations.Add(conversation);
        }

        var working = BuildHistory(conversation);
        working.Add(LlmMessage.User(userMessage));

        db.Messages.Add(new ChatMessage { ConversationId = conversation.Id, Role = ChatRole.User, Content = userMessage });

        var systemPrompt = BuildSystemPrompt();
        var toolDefs = tools.ToLlmDefinitions();

        var trace = new List<ToolTraceEntry>();
        string finalReply = "";
        int inTok = 0, outTok = 0;

        for (var iteration = 0; iteration < MaxToolIterations; iteration++)
        {
            var response = await llm.CompleteAsync(new LlmRequest
            {
                Messages = working,
                SystemPrompt = systemPrompt,
                Tools = toolDefs,
                MaxTokens = 1500,
            }, ct);

            inTok += response.InputTokens;
            outTok += response.OutputTokens;

            if (!response.WantsToolUse)
            {
                finalReply = response.Text ?? "";
                break;
            }

            working.Add(new LlmMessage { Role = LlmRole.Assistant, Text = response.Text, ToolCalls = response.ToolCalls });

            var results = new List<LlmToolResult>();
            foreach (var call in response.ToolCalls)
            {
                var (content, isError) = await RunToolAsync(call, ct);
                results.Add(new LlmToolResult(call.Id, content, isError));
                trace.Add(new ToolTraceEntry(call.Name, call.ArgumentsJson, content, isError));
            }
            working.Add(new LlmMessage { Role = LlmRole.Tool, ToolResults = results });

            if (!string.IsNullOrWhiteSpace(response.Text)) finalReply = response.Text!;
        }

        if (string.IsNullOrWhiteSpace(finalReply))
            finalReply = "Sorry - I wasn't able to complete that. Could you rephrase?";

        db.Messages.Add(new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = ChatRole.Assistant,
            Content = finalReply,
            ToolTrace = trace.Count > 0 ? JsonSerializer.Serialize(trace) : null,
        });
        conversation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new ChatTurnResult(conversation.Id, finalReply, trace, inTok, outTok);
    }

    private async Task<(string content, bool isError)> RunToolAsync(LlmToolCall call, CancellationToken ct)
    {
        var tool = tools.Find(call.Name);
        if (tool is null) return ($"Unknown tool '{call.Name}'.", true);

        var args = call.Arguments();

        if (tool.RequiresConfirmation
            && !(args.TryGetPropertyValue("confirmed", out var c) && c is not null && c.GetValueKind() == JsonValueKind.True))
        {
            return (ToolExecutionResult.NeedsConfirmation($"tool '{call.Name}' with {call.ArgumentsJson}").Content, false);
        }

        try
        {
            var result = await tool.ExecuteAsync(new ToolExecutionContext(args), ct);
            return (result.Content, result.IsError);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Tool {Tool} threw", call.Name);
            return ($"Tool '{call.Name}' failed: {ex.Message}", true);
        }
    }

    private static List<LlmMessage> BuildHistory(Conversation conversation) =>
        conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .TakeLast(HistoryTurns)
            .Select(m => new LlmMessage
            {
                Role = m.Role == ChatRole.User ? LlmRole.User : LlmRole.Assistant,
                Text = m.Content,
            })
            .ToList();

    private static string BuildSystemPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are the user's personal travel assistant. You help plan trips, build itineraries, and track packing lists.");
        sb.AppendLine("You are practical, organized, and concise.");
        sb.AppendLine($"Current time - UTC: {DateTime.UtcNow:dddd yyyy-MM-dd HH:mm}Z");
        sb.AppendLine();
        sb.AppendLine("Guidelines:");
        sb.AppendLine("- Use tools to read/write real data. Never invent trip, itinerary, or packing item ids.");
        sb.AppendLine("- When adding itinerary or packing items, first find the right trip_id via get_trips if the user doesn't give one explicitly.");
        sb.AppendLine("- For relative dates ('today', 'next month'), call current_datetime first.");
        sb.AppendLine("- Before any tool whose result says CONFIRMATION REQUIRED, stop and ask the user in plain language. Only after they clearly say yes, call the tool again with confirmed=true.");
        sb.AppendLine("- Keep replies short and clear. Confirm what you did (\"Added flight to Tokyo on 2026-10-01\").");
        return sb.ToString();
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrWhiteSpace(s) ? "New conversation"
           : s.Length <= max ? s : s[..max].TrimEnd() + "...";
}
