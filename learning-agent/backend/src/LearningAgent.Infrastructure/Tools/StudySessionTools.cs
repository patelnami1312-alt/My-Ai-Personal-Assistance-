using System.Text;
using System.Text.Json.Nodes;
using LearningAgent.Core.Abstractions;
using LearningAgent.Core.Entities;
using LearningAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LearningAgent.Infrastructure.Tools;

internal static class StudySessionFormat
{
    public static string Line(StudySession s) =>
        $"- {s.Date:yyyy-MM-dd} {s.Topic} (id={s.Id}, {s.DurationMinutes} min" +
        (s.CourseId is { } cid ? $", course_id={cid}" : "") + ")" +
        (string.IsNullOrWhiteSpace(s.Notes) ? "" : $"\n    {s.Notes}");
}

public class LogStudySessionTool(LearningAgentDbContext db) : ITool
{
    public string Name => "log_study_session";
    public string Description => "Record a study session.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("topic", "Topic studied, e.g. 'React hooks'", required: true)
        .Int("duration_minutes", "Duration in minutes", required: true)
        .Str("course_id", "Optional course id (GUID) this session belongs to")
        .Str("date", "ISO-8601 date/time. Defaults to now.")
        .Str("notes", "Optional notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var topic = ctx.Arguments.GetString("topic");
        if (string.IsNullOrWhiteSpace(topic)) return ToolExecutionResult.Error("topic is required");
        var duration = ctx.Arguments.GetInt("duration_minutes");
        if (duration is null || duration <= 0) return ToolExecutionResult.Error("duration_minutes is required and must be positive");

        Guid? courseId = null;
        var courseIdRaw = ctx.Arguments.GetString("course_id");
        if (!string.IsNullOrWhiteSpace(courseIdRaw))
        {
            if (!Guid.TryParse(courseIdRaw, out var parsed)) return ToolExecutionResult.Error("course_id must be a valid GUID");
            courseId = parsed;
        }

        var s = new StudySession
        {
            Topic = topic.Trim(),
            DurationMinutes = duration.Value,
            CourseId = courseId,
            Notes = ctx.Arguments.GetString("notes"),
            Date = ctx.Arguments.GetDateUtc("date") ?? DateTime.UtcNow,
        };
        db.StudySessions.Add(s);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Logged study session.\n{StudySessionFormat.Line(s)}");
    }
}

public class GetStudySessionsTool(LearningAgentDbContext db) : ITool
{
    public string Name => "get_study_sessions";
    public string Description => "List logged study sessions, most recent first, optionally filtered by date range or course.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("from", "Range start (ISO-8601 date)")
        .Str("to", "Range end (ISO-8601 date)")
        .Str("course_id", "Filter by course id (GUID)")
        .Int("limit", "Max rows (default 20)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.StudySessions.AsQueryable();
        if (ctx.Arguments.GetDateUtc("from") is { } from) q = q.Where(s => s.Date >= from);
        if (ctx.Arguments.GetDateUtc("to") is { } to) q = q.Where(s => s.Date <= to);
        if (Guid.TryParse(ctx.Arguments.GetString("course_id"), out var cid)) q = q.Where(s => s.CourseId == cid);

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 20, 1, 100);
        var rows = await q.OrderByDescending(s => s.Date).Take(limit).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching study sessions.");

        var sb = new StringBuilder($"{rows.Count} study session(s):\n");
        foreach (var s in rows) sb.AppendLine(StudySessionFormat.Line(s));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class GetStudySummaryTool(LearningAgentDbContext db) : ITool
{
    public string Name => "get_study_summary";
    public string Description => "Get total minutes studied in a date range, broken down by topic.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("from", "Range start (ISO-8601 date). Defaults to 7 days ago.")
        .Str("to", "Range end (ISO-8601 date). Defaults to now.")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var from = ctx.Arguments.GetDateUtc("from") ?? DateTime.UtcNow.AddDays(-7);
        var to = ctx.Arguments.GetDateUtc("to") ?? DateTime.UtcNow;

        var rows = await db.StudySessions
            .Where(s => s.Date >= from && s.Date <= to)
            .ToListAsync(ct);

        if (rows.Count == 0) return ToolExecutionResult.Ok($"No study sessions between {from:yyyy-MM-dd} and {to:yyyy-MM-dd}.");

        var totalMinutes = rows.Sum(s => s.DurationMinutes);
        var byTopic = rows.GroupBy(s => s.Topic)
            .Select(g => (Topic: g.Key, Minutes: g.Sum(x => x.DurationMinutes)))
            .OrderByDescending(g => g.Minutes)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"Study summary {from:yyyy-MM-dd} to {to:yyyy-MM-dd}: {totalMinutes} total minutes across {rows.Count} session(s).");
        sb.AppendLine("By topic:");
        foreach (var (topic, minutes) in byTopic)
            sb.AppendLine($"- {topic}: {minutes} min");
        return ToolExecutionResult.Ok(sb.ToString());
    }
}
