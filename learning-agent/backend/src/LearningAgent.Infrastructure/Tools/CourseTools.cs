using System.Text;
using System.Text.Json.Nodes;
using LearningAgent.Core.Abstractions;
using LearningAgent.Core.Entities;
using LearningAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LearningAgent.Infrastructure.Tools;

internal static class CourseFormat
{
    public static string Line(Course c) =>
        $"- [{c.Status}] {c.Title} (id={c.Id}" +
        (string.IsNullOrWhiteSpace(c.Subject) ? "" : $", subject={c.Subject}") +
        $", started {c.StartedAt:yyyy-MM-dd})" +
        (string.IsNullOrWhiteSpace(c.Notes) ? "" : $"\n    {c.Notes}");
}

public class AddCourseTool(LearningAgentDbContext db) : ITool
{
    public string Name => "add_course";
    public string Description => "Add a new course or subject the user is studying.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("title", "Course title, e.g. 'Introduction to Algorithms'", required: true)
        .Str("subject", "Subject area, e.g. 'Computer Science'")
        .Str("notes", "Optional notes")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var title = ctx.Arguments.GetString("title");
        if (string.IsNullOrWhiteSpace(title)) return ToolExecutionResult.Error("title is required");

        var c = new Course
        {
            Title = title.Trim(),
            Subject = ctx.Arguments.GetString("subject"),
            Notes = ctx.Arguments.GetString("notes"),
            Status = CourseStatus.Active,
            StartedAt = DateTime.UtcNow,
        };
        db.Courses.Add(c);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Added course.\n{CourseFormat.Line(c)}");
    }
}

public class GetCoursesTool(LearningAgentDbContext db) : ITool
{
    public string Name => "get_courses";
    public string Description => "List courses, optionally filtered by status.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("status", "Filter by status", enumValues: ["Active", "Completed", "Paused"])
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Courses.AsQueryable();
        if (Enum.TryParse<CourseStatus>(ctx.Arguments.GetString("status"), true, out var s))
            q = q.Where(c => c.Status == s);

        var rows = await q.OrderBy(c => c.Status).ThenByDescending(c => c.StartedAt).ToListAsync(ct);
        if (rows.Count == 0) return ToolExecutionResult.Ok("No matching courses.");

        var sb = new StringBuilder($"{rows.Count} course(s):\n");
        foreach (var c in rows) sb.AppendLine(CourseFormat.Line(c));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class UpdateCourseStatusTool(LearningAgentDbContext db) : ITool
{
    public string Name => "update_course_status";
    public string Description => "Update a course's status, e.g. mark it Completed or Paused.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Course id (GUID)", required: true)
        .Str("status", "New status", required: true, enumValues: ["Active", "Completed", "Paused"])
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid course id is required");
        var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return ToolExecutionResult.Error("course not found");

        if (!Enum.TryParse<CourseStatus>(ctx.Arguments.GetString("status"), true, out var s))
            return ToolExecutionResult.Error("valid status is required");

        c.Status = s;
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Updated course.\n{CourseFormat.Line(c)}");
    }
}
