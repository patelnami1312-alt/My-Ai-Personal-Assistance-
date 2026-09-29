using System.Text.Json;
using System.Text.Json.Serialization;
using Assistant.Core.Abstractions;
using Assistant.Core.Entities;
using Assistant.Core.Llm;
using Assistant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Assistant.Infrastructure.Tools;

namespace Assistant.Infrastructure.Agents.Personal;

/// <summary>
/// After each exchange, asks the model whether anything durable was revealed about the user
/// and stores it. Runs out-of-band so it never adds latency to the chat response.
/// </summary>
public class MemoryExtractionService(
    AssistantDbContext db,
    ILlmProvider llm,
    ILogger<MemoryExtractionService> log) : IMemoryExtractor
{
    private const string SystemPrompt = """
        You maintain the long-term memory of a personal assistant.
        Given one exchange between the user and the assistant, extract ONLY durable facts
        worth recalling weeks later: stable preferences, goals, relationships, recurring
        context, important personal details.

        Do NOT store: one-off logistics, task/calendar content already saved elsewhere,
        transient states ("I'm tired"), the assistant's own words, or anything already obvious.

        Always phrase "content" starting with "The user " (never a name) so entries stay
        consistent and de-duplicable.

        Reply with a JSON array (possibly empty). Each item:
          { "content": "The user <standalone statement>",
            "category": "Fact|Preference|Goal|Relationship|Event",
            "keywords": ["..."],
            "confidence": 0.0-1.0 }
        Output the JSON array and nothing else.
        """;

    public async Task ExtractAndStoreAsync(Guid userId, string conversationId, string userText, string assistantText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userText)) return;

        var response = await llm.CompleteAsync(new LlmRequest
        {
            SystemPrompt = SystemPrompt,
            MaxTokens = 600,
            Temperature = 0,
            Messages = new[]
            {
                LlmMessage.User($"USER: {userText}\n\nASSISTANT: {assistantText}"),
            },
        }, ct);

        var candidates = Parse(response.Text);
        if (candidates.Count == 0) return;

        var existing = (await db.Memories
                .Where(m => m.UserId == userId)
                .Select(m => m.Content)
                .ToListAsync(ct))
            .Select(Normalize)
            .ToList();

        var added = 0;
        foreach (var c in candidates)
        {
            if (string.IsNullOrWhiteSpace(c.Content) || c.Confidence < 0.5) continue;
            var key = Normalize(c.Content);
            // Skip exact or near-duplicates (one string contained in the other).
            if (existing.Any(e => e == key || e.Contains(key) || key.Contains(e))) continue;
            existing.Add(key);

            db.Memories.Add(new MemoryItem
            {
                UserId = userId,
                Content = c.Content.Trim(),
                Category = Enum.TryParse<MemoryCategory>(c.Category, true, out var cat) ? cat : MemoryCategory.Fact,
                Keywords = c.Keywords ?? new(),
                Confidence = Math.Clamp(c.Confidence, 0, 1),
                SourceConversationId = conversationId,
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            log.LogInformation("Stored {Count} new memory(ies) for user {User}", added, userId);
        }
    }

    private static string Normalize(string s)
        => string.Join(' ', s.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                 .TrimEnd('.', '!', '?');

    private static List<Candidate> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new();
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end <= start) return new();
        try
        {
            return JsonSerializer.Deserialize<List<Candidate>>(text[start..(end + 1)],
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new();
        }
        catch
        {
            return new();
        }
    }

    private sealed class Candidate
    {
        [JsonPropertyName("content")] public string Content { get; set; } = "";
        [JsonPropertyName("category")] public string Category { get; set; } = "Fact";
        [JsonPropertyName("keywords")] public List<string>? Keywords { get; set; }
        [JsonPropertyName("confidence")] public double Confidence { get; set; } = 0.6;
    }
}
