using System.Text;
using System.Text.Json.Nodes;
using Assistant.Core.Abstractions;
using Assistant.Core.Entities;
using Assistant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Assistant.Infrastructure.Tools;

namespace Assistant.Infrastructure.Agents.Personal;

internal static class TaskFormat
{
    public static string Line(TaskItem t) =>
        $"- [{t.Status}] {t.Title} (id={t.Id}, priority={t.Priority}" +
        (t.DueAt is { } d ? $", due={d:yyyy-MM-dd HH:mm}Z" : "") + ")" +
        (string.IsNullOrWhiteSpace(t.Description) ? "" : $"\n    {t.Description}");
}

public class CreateTaskTool(AssistantDbContext db) : ITool
{
    public string Name => "create_task";
    public string Description => "Add a new task / to-do item for the user.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("title", "Short title of the task", required: true)
        .Str("description", "Optional longer details")
        .Str("priority", "Task priority", enumValues: ["Low", "Medium", "High"])
        .Str("due_at", "Optional due date/time, ISO-8601. Bare local times are read in the user's timezone.")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var title = ctx.Arguments.GetString("title");
        if (string.IsNullOrWhiteSpace(title)) return ToolExecutionResult.Error("title is required");

        var task = new TaskItem
        {
            UserId = ctx.UserId,
            Title = title.Trim(),
            Description = ctx.Arguments.GetString("description"),
            Priority = Enum.TryParse<TaskPriority>(ctx.Arguments.GetString("priority"), true, out var p) ? p : TaskPriority.Medium,
            DueAt = ctx.Arguments.GetDateTimeUtc("due_at", ctx.TimeZone),
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Created task.\n{TaskFormat.Line(task)}");
    }
}

public class GetTasksTool(AssistantDbContext db) : ITool
{
    public string Name => "get_tasks";
    public string Description => "List the user's tasks, optionally filtered by status or due window.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("status", "Filter by status", enumValues: ["Todo", "InProgress", "Done"])
        .Bool("due_today", "Only tasks due today or overdue")
        .Int("limit", "Max rows (default 50)")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        var q = db.Tasks.Where(t => t.UserId == ctx.UserId);

        if (Enum.TryParse<TaskState>(ctx.Arguments.GetString("status"), true, out var s))
            q = q.Where(t => t.Status == s);

        if (ctx.Arguments.GetBool("due_today"))
        {
            var endOfToday = DateTime.UtcNow.Date.AddDays(1);
            q = q.Where(t => t.DueAt != null && t.DueAt < endOfToday && t.Status != TaskState.Done);
        }

        var limit = Math.Clamp(ctx.Arguments.GetInt("limit") ?? 50, 1, 200);
        var tasks = await q.OrderBy(t => t.Status).ThenBy(t => t.DueAt ?? DateTime.MaxValue)
            .ThenByDescending(t => t.Priority).Take(limit).ToListAsync(ct);

        if (tasks.Count == 0) return ToolExecutionResult.Ok("No matching tasks.");
        var sb = new StringBuilder($"{tasks.Count} task(s):\n");
        foreach (var t in tasks) sb.AppendLine(TaskFormat.Line(t));
        return ToolExecutionResult.Ok(sb.ToString());
    }
}

public class UpdateTaskTool(AssistantDbContext db) : ITool
{
    public string Name => "update_task";
    public string Description => "Change fields of an existing task (title, description, priority, due date, or status).";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Task id (GUID)", required: true)
        .Str("title", "New title")
        .Str("description", "New description")
        .Str("priority", "New priority", enumValues: ["Low", "Medium", "High"])
        .Str("due_at", "New due date/time (ISO-8601), or 'none' to clear")
        .Str("status", "New status", enumValues: ["Todo", "InProgress", "Done"])
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid task id is required");
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == ctx.UserId, ct);
        if (task is null) return ToolExecutionResult.Error("task not found");

        if (ctx.Arguments.GetString("title") is { } title && !string.IsNullOrWhiteSpace(title)) task.Title = title.Trim();
        if (ctx.Arguments.GetString("description") is { } desc) task.Description = desc;
        if (Enum.TryParse<TaskPriority>(ctx.Arguments.GetString("priority"), true, out var p)) task.Priority = p;

        var dueRaw = ctx.Arguments.GetString("due_at");
        if (string.Equals(dueRaw, "none", StringComparison.OrdinalIgnoreCase)) task.DueAt = null;
        else if (ctx.Arguments.GetDateTimeUtc("due_at", ctx.TimeZone) is { } due) task.DueAt = due;

        if (Enum.TryParse<TaskState>(ctx.Arguments.GetString("status"), true, out var st))
        {
            task.Status = st;
            task.CompletedAt = st == TaskState.Done ? DateTime.UtcNow : null;
        }
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Updated task.\n{TaskFormat.Line(task)}");
    }
}

public class CompleteTaskTool(AssistantDbContext db) : ITool
{
    public string Name => "complete_task";
    public string Description => "Mark a task as done.";
    public bool RequiresConfirmation => false;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Task id (GUID)", required: true)
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid task id is required");
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == ctx.UserId, ct);
        if (task is null) return ToolExecutionResult.Error("task not found");
        task.Status = TaskState.Done;
        task.CompletedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Completed: {task.Title}");
    }
}

public class DeleteTaskTool(AssistantDbContext db) : ITool
{
    public string Name => "delete_task";
    public string Description => "Permanently delete a task. Destructive - the user must confirm first.";
    public bool RequiresConfirmation => true;
    public JsonObject ParametersSchema => Schema.Object()
        .Str("id", "Task id (GUID)", required: true)
        .Bool("confirmed", "Set true only after the user has explicitly confirmed the deletion")
        .Build();

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext ctx, CancellationToken ct = default)
    {
        if (!Guid.TryParse(ctx.Arguments.GetString("id"), out var id))
            return ToolExecutionResult.Error("valid task id is required");
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == ctx.UserId, ct);
        if (task is null) return ToolExecutionResult.Error("task not found");
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return ToolExecutionResult.Ok($"Deleted task: {task.Title}");
    }
}
