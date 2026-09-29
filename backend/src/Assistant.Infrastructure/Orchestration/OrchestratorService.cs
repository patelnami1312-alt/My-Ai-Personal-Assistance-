using System.Text;
using System.Text.Json;
using Assistant.Core.Abstractions;
using Assistant.Core.Dtos;
using Assistant.Core.Entities;
using Assistant.Core.Llm;
using Assistant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Assistant.Infrastructure.Orchestration;

/// <summary>
/// The brain. Takes a user message, runs a bounded tool-use loop against the LLM,
/// persists the exchange, and triggers background memory extraction.
/// </summary>
public class OrchestratorService(
    AssistantDbContext db,
    ILlmProvider llm,
    IToolRegistry tools,
    IServiceScopeFactory scopeFactory,
    ILogger<OrchestratorService> log) : IOrchestrator
{
    private const int MaxToolIterations = 6;
    private const int HistoryTurns = 16;

    public async Task<ChatTurnResult> HandleAsync(Guid userId, Guid? conversationId, string userMessage, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new InvalidOperationException("user profile missing");

        var conversation = conversationId is { } cid
            ? await db.Conversations.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == cid && c.UserId == userId, ct)
            : null;

        if (conversation is null)
        {
            conversation = new Conversation
            {
                UserId = userId,
                Title = Truncate(userMessage, 60),
            };
            db.Conversations.Add(conversation);
        }

        // History for the LLM = prior turns already persisted, then this new user turn.
        var working = BuildHistory(conversation);
        working.Add(LlmMessage.User(userMessage));

        // Persist via the DbSet (not the nav collection): once the conversation row exists,
        // adding through conversation.Messages makes EF treat the pre-keyed entity as an update.
        db.Messages.Add(new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = ChatRole.User,
            Content = userMessage,
        });

        var systemPrompt = await BuildSystemPromptAsync(user, ct);
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

            working.Add(new LlmMessage
            {
                Role = LlmRole.Assistant,
                Text = response.Text,
                ToolCalls = response.ToolCalls,
            });

            var results = new List<LlmToolResult>();
            foreach (var call in response.ToolCalls)
            {
                var (content, isError) = await RunToolAsync(call, userId, conversation.Id, user.TimeZone, ct);
                results.Add(new LlmToolResult(call.Id, content, isError));
                trace.Add(new ToolTraceEntry(call.Name, call.ArgumentsJson, content, isError));
            }
            working.Add(new LlmMessage { Role = LlmRole.Tool, ToolResults = results });

            // Keep any interim narration as the fallback reply.
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

        FireMemoryExtraction(userId, conversation.Id, userMessage, finalReply);

        return new ChatTurnResult(conversation.Id, finalReply, trace, inTok, outTok);
    }

    private async Task<(string content, bool isError)> RunToolAsync(
        LlmToolCall call, Guid userId, Guid conversationId, string tz, CancellationToken ct)
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
            var result = await tool.ExecuteAsync(new ToolExecutionContext(userId, conversationId.ToString(), args, tz), ct);
            return (result.Content, result.IsError);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Tool {Tool} threw", call.Name);
            return ($"Tool '{call.Name}' failed: {ex.Message}", true);
        }
    }

    private static List<LlmMessage> BuildHistory(Conversation conversation)
    {
        // Replay only final user/assistant text turns - tool_use blocks are internal to
        // each HandleAsync call and don't need to persist across turns.
        return conversation.Messages
            .Where(m => m.Role is ChatRole.User or ChatRole.Assistant)
            .OrderBy(m => m.CreatedAt)
            .TakeLast(HistoryTurns)
            .Select(m => new LlmMessage
            {
                Role = m.Role == ChatRole.User ? LlmRole.User : LlmRole.Assistant,
                Text = m.Content,
            })
            .ToList();
    }

    private async Task<string> BuildSystemPromptAsync(UserProfile user, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        string localNow;
        try
        {
            var tzi = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZone);
            localNow = $"{TimeZoneInfo.ConvertTimeFromUtc(now, tzi):dddd yyyy-MM-dd HH:mm} ({user.TimeZone})";
        }
        catch { localNow = "unknown (timezone not set)"; }

        var memories = await db.Memories
            .Where(m => m.UserId == user.Id)
            .OrderByDescending(m => m.LastUsedAt)
            .Take(40)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("You are the user's personal AI assistant. You are warm, concise, and proactive.");
        sb.AppendLine("You help manage tasks, calendar, notes, and long-term memory, and you answer questions.");
        sb.AppendLine();
        sb.AppendLine($"User: {user.Name}");
        sb.AppendLine($"Timezone: {user.TimeZone}");
        sb.AppendLine($"Current time - UTC: {now:dddd yyyy-MM-dd HH:mm}Z; user local: {localNow}");
        if (user.Preferences.Count > 0)
        {
            sb.AppendLine("Stated preferences:");
            foreach (var p in user.Preferences) sb.AppendLine($"  - {p}");
        }
        if (memories.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("What you remember about the user:");
            foreach (var m in memories) sb.AppendLine($"  - [{m.Category}] {m.Content}");
        }
        sb.AppendLine();
        sb.AppendLine("Guidelines:");
        sb.AppendLine("- Use tools to read/write real data. Never invent task ids, events, or notes.");
        sb.AppendLine("- For relative dates ('tomorrow', 'next Friday'), call current_datetime first, then compute.");
        sb.AppendLine("- Before any tool whose result says CONFIRMATION REQUIRED, stop and ask the user in plain language. Only after they clearly say yes, call the tool again with confirmed=true.");
        sb.AppendLine("- Durable facts the user shares are saved to memory automatically after the turn - just acknowledge naturally (\"I'll remember that\"); there is no tool to call. Use recall to search past memories on demand.");
        sb.AppendLine("- Keep replies short and skimmable. Confirm what you did (\"Added 'Call recruiter' for tomorrow 3pm\").");
        sb.AppendLine("- If a tool isn't available for what's asked, say so plainly.");
        return sb.ToString();
    }

    private void FireMemoryExtraction(Guid userId, Guid conversationId, string userText, string assistantText)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var extractor = scope.ServiceProvider.GetRequiredService<IMemoryExtractor>();
                await extractor.ExtractAndStoreAsync(userId, conversationId.ToString(), userText, assistantText);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Background memory extraction failed");
            }
        });
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrWhiteSpace(s) ? "New conversation"
           : s.Length <= max ? s : s[..max].TrimEnd() + "…";
}
