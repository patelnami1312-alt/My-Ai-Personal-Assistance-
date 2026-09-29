using System.Text;
using System.Text.Json.Nodes;
using LearningAgent.Core.Abstractions;
using LearningAgent.Core.Entities;
using LearningAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LearningAgent.Infrastructure.Tools;

internal static class NoteFormat
{
    public static string Line(LearningNote n) =>
        $"- {n.Title} (id={n.Id}, {n.CreatedAt:yyyy-MM-dd}" +
        (n.Tags.Count > 0 ? $", tags={string.Join(", ", n.Tags)}" : "") + ")";
}

public class AddNoteTool(LearningAgentDbContext db) : ITool
{
    public string Name => "add_note";
    public string Description => "Save a learning note.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("title", "Note title", required: true)
        .Str("content", "Note content", required: true)
        .Str("tags", "Comma-separated tags, e.g. 'react,frontend'")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var title = ctx.Arguments.GetString("title");
        var content = ctx.Arguments.GetString("content");
        if (string.IsNullOrWhiteSpace(title)) return ToolExecutionResult.Error("title is required");
        if (string.IsNullOrWhiteSpace(content)) return ToolExecutionResult.Error("content is required");

        var tagsRaw = ctx.Arguments.GetString("tags");
        var tags = string.IsNullOrWhiteSpace(tagsRaw)
            ? new List<string>()
            : tagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var n = new LearningNote
        {
            Title = title.Trim(),
            Content = content.Trim(),
            Tags = tags,
        };
        db.Notes.Add(n);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Saved note.\n{NoteFormat.Line(n)}");
    }
}

public class SearchNotesTool(LearningAgentDbContext db) : ITool
{
    public string Name => "search_notes";
    public string Description => "Search learning notes by keyword (matches title, content, or tags). Leave query empty to get recent notes.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("query", "Keyword to search for")
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Notes.AsQueryable();
        var query = ctx.Arguments.GetString("query");
        if (!string.IsNullOrWhiteSpace(query))
        {
            var like = $"%{query}%";
            q = q.Where(n => EF.Functions.ILike(n.Title, like)
                           || EF.Functions.ILike(n.Content, like)
                           || n.Tags.Any(t => EF.Functions.ILike(t, like)));
        }

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 100);
        var rows = await q.OrderByDescending(n => n.UpdatedAt).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching notes.");

        var sb = new StringBuilder($"{rows.Count} note(s):\n");
        foreach (var n in rows) sb.AppendLine(NoteFormat.Line(n));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class DeleteNoteTool(LearningAgentDbContext db) : ITool
{
    public string Name => "delete_note";
    public string Description => "Permanently delete a learning note. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Note id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid note id is required");
        var n = await db.Notes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null) return ToolExecutionResult.Error("note not found");
        db.Notes.Remove(n);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted note: {n.Title}");
    }
}
