using System.Text;
using System.Text.Json;
using HomeAgent.Core.Abstractions;
using HomeAgent.Core.Dtos;
using HomeAgent.Core.Entities;
using HomeAgent.Core.Llm;
using HomeAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeAgent.Infrastructure.Orchestration;

/// <summary>
/// The brain. Takes a user message, runs a bounded tool-use loop against the LLM,
/// and persists the exchange.
/// </summary>
public class OrchestratorService(
    HomeAgentDbContext db,
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
        sb.AppendLine("You are the user's home management assistant. You help track chores, home maintenance schedules, and household inventory/supplies, and remind them what's due or running low.");
        sb.AppendLine($"Current time - UTC: {DateTime.UtcNow:dddd yyyy-MM-dd HH:mm}Z");
        sb.AppendLine();
        sb.AppendLine("Guidelines:");
        sb.AppendLine("- Use tools to read/write real data. Never invent chore, maintenance, or inventory ids.");
        sb.AppendLine("- For relative dates ('today', 'this week'), call current_datetime first.");
        sb.AppendLine("- Before any tool whose result says CONFIRMATION REQUIRED, stop and ask the user in plain language. Only after they clearly say yes, call the tool again with confirmed=true.");
        sb.AppendLine("- Keep replies short and practical. Confirm what you logged (\"Added chore: take out the trash, due Friday\").");
        sb.AppendLine("- Proactively mention chores due today, overdue chores, upcoming maintenance, or low inventory when relevant.");
        return sb.ToString();
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrWhiteSpace(s) ? "New conversation"
           : s.Length <= max ? s : s[..max].TrimEnd() + "...";
}
