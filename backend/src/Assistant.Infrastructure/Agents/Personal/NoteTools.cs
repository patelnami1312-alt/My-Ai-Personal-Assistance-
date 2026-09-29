using System.Text;
using System.Text.Json.Nodes;
using Assistant.Core.Abstractions;
using Assistant.Core.Entities;
using Assistant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Assistant.Infrastructure.Tools;

namespace Assistant.Infrastructure.Agents.Personal;

public class CreateNoteTool(AssistantDbContext db) : ITool
{
    public string Name => "create_note";
    public string Description => "Save a free-form note for the user (ideas, snippets, things to remember verbatim).";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("title", "Short title", required: true)
        .Str("content", "Note body", required: true)
        .Str("tags", "Comma-separated tags")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var title = ctx.Arguments.GetString("title");
        var content = ctx.Arguments.GetString("content");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            return ToolExecutionResult.Error("title and content are required");

        var note = new Note
        {
            UserId = ctx.UserId,
            Title = title.Trim(),
            Content = content.Trim(),
            Tags = (ctx.Arguments.GetString("tags") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
        };
        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Saved note '{note.Title}' (id={note.Id}).");
    }
}

public class SearchNotesTool(AssistantDbContext db) : ITool
{
    public string Name => "search_notes";
    public string Description => "Find the user's notes by keyword in the title, body, or tags.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("query", "Keyword(s) to search for. Empty returns the most recent notes.")
        .Int("limit", "Max rows (default 10)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var query = (ctx.Arguments.GetString("query") ?? "").Trim();
        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 10, 1, 50);

        var q = db.Notes.Where(n => n.UserId == ctx.UserId);
        if (query.Length > 0)
        {
            var like = $"%{query}%";
            q = q.Where(n => EF.Functions.ILike(n.Title, like)
                || EF.Functions.ILike(n.Content, like)
                || n.Tags.Any(t => EF.Functions.ILike(t, like)));
        }

        var notes = await q.OrderByDescending(n => n.UpdatedAt).Take(limit).ToListAsync(ct);
        if (notes.Count == 0) return ToolExecutionResult.Ok("No matching notes.");

        var sb = new StringBuilder($"{notes.Count} note(s):\n");
        foreach (var n in notes)
        {
            sb.AppendLine($"- {n.Title} (id={n.Id}, {n.CreatedAt:yyyy-MM-dd})"
                + (n.Tags.Count > 0 ? $" [{string.Join(", ", n.Tags)}]" : ""));
            sb.AppendLine($"    {n.Content}");
        }
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class DeleteNoteTool(AssistantDbContext db) : ITool
{
    public string Name => "delete_note";
    public string Description => "Permanently delete a note. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Note id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid note id is required");
        var note = await db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == ctx.UserId, ct);
        if (note is null) return ToolExecutionResult.Error("note not found");
        db.Notes.Remove(note);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted note: {note.Title}");
    }
}
