using System.Text;
using System.Text.Json.Nodes;
using Assistant.Core.Abstractions;
using Assistant.Core.Entities;
using Assistant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Assistant.Infrastructure.Tools;

namespace Assistant.Infrastructure.Agents.Personal;

// Note: there is deliberately no "remember" tool. Long-term memories are written by the
// out-of-band MemoryExtractionService (single writer), which avoids the model and the
// extractor both storing the same fact with slightly different wording. The model can still
// acknowledge ("Got it, I'll remember that") - the extractor picks it up after the turn.

public class RecallTool(AssistantDbContext db) : ITool
{
    public string Name => "recall";
    public string Description => "Search stored long-term memories about the user by keyword.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("query", "Keyword(s). Empty returns all memories.")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var query = (ctx.Arguments.GetString("query") ?? "").Trim();
        var q = db.Memories.Where(m => m.UserId == ctx.UserId);
        if (query.Length > 0)
        {
            var like = $"%{query}%";
            q = q.Where(m => EF.Functions.ILike(m.Content, like) || m.Keywords.Any(k => EF.Functions.ILike(k, like)));
        }
        var memories = await q.OrderByDescending(m => m.LastUsedAt).Take(50).ToListAsync(ct);
        if (memories.Count == 0) return ToolExecutionResult.Ok("Nothing remembered matching that.");

        foreach (var m in memories) m.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var sb = new StringBuilder($"{memories.Count} memory(ies):\n");
        foreach (var m in memories) sb.AppendLine($"- [{m.Category}] {m.Content} (id={m.Id})");
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class ForgetTool(AssistantDbContext db) : ITool
{
    public string Name => "forget";
    public string Description => "Delete a stored memory. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Memory id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid memory id is required");
        var mem = await db.Memories.FirstOrDefaultAsync(m => m.Id == id && m.UserId == ctx.UserId, ct);
        if (mem is null) return ToolExecutionResult.Error("memory not found");
        db.Memories.Remove(mem);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Forgot: {mem.Content}");
    }
}

public class UpdateProfileTool(AssistantDbContext db) : ITool
{
    public string Name => "update_profile";
    public string Description => "Update the user's profile: display name, timezone (IANA id like 'America/New_York'), language, or add a preference.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("name", "Display name / what to call the user")
        .Str("timezone", "IANA timezone id")
        .Str("language", "Preferred language code")
        .Str("add_preference", "A preference statement to append")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == ctx.UserId, ct);
        if (user is null) return ToolExecutionResult.Error("profile not found");

        if (ctx.Arguments.GetString("name") is { } n && !string.IsNullOrWhiteSpace(n)) user.Name = n.Trim();
        if (ctx.Arguments.GetString("timezone") is { } tz && !string.IsNullOrWhiteSpace(tz))
        {
            try { _ = TimeZoneInfo.FindSystemTimeZoneById(tz.Trim()); user.TimeZone = tz.Trim(); }
            catch { return ToolExecutionResult.Error($"unknown timezone id: {tz}"); }
        }
        if (ctx.Arguments.GetString("language") is { } lang && !string.IsNullOrWhiteSpace(lang)) user.Language = lang.Trim();
        if (ctx.Arguments.GetString("add_preference") is { } pref && !string.IsNullOrWhiteSpace(pref)
            && !user.Preferences.Contains(pref.Trim()))
            user.Preferences.Add(pref.Trim());

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Profile updated (name={user.Name}, tz={user.TimeZone}, lang={user.Language}).");
    }
}
