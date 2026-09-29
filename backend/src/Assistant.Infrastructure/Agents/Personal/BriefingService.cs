using System.Text;
using Assistant.Core.Abstractions;
using Assistant.Core.Dtos;
using Assistant.Core.Entities;
using Assistant.Core.Llm;
using Assistant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Assistant.Infrastructure.Tools;

namespace Assistant.Infrastructure.Agents.Personal;

/// <summary>Assembles the morning briefing: today's events + open/overdue tasks + a short spoken-style summary.</summary>
public class BriefingService(
    AssistantDbContext db,
    ILlmProvider llm,
    ILogger<BriefingService> log) : IBriefingService
{
    public async Task<DailyBriefing> BuildAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new InvalidOperationException("user profile missing");

        TimeZoneInfo tzi;
        try { tzi = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZone); }
        catch { tzi = TimeZoneInfo.Utc; }

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
        var localMidnight = localNow.Date;
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localMidnight, DateTimeKind.Unspecified), tzi);
        var dayEndUtc = dayStartUtc.AddDays(1);

        var events = await db.Events
            .Where(e => e.UserId == userId && e.StartsAt < dayEndUtc && e.EndsAt > dayStartUtc)
            .OrderBy(e => e.StartsAt)
            .Select(e => new BriefingEvent(e.Id, e.Title, e.StartsAt, e.EndsAt, e.Location, e.AllDay))
            .ToListAsync(ct);

        var openTasks = await db.Tasks
            .Where(t => t.UserId == userId && t.Status != TaskState.Done)
            .OrderBy(t => t.DueAt ?? DateTime.MaxValue)
            .ThenByDescending(t => t.Priority)
            .ToListAsync(ct);

        var overdue = openTasks.Count(t => t.DueAt is { } d && d < DateTime.UtcNow);
        var todaysTasks = openTasks
            .Where(t => t.DueAt is null || t.DueAt < dayEndUtc)
            .Take(10)
            .Select(t => new BriefingTask(t.Id, t.Title, t.Priority.ToString(), t.DueAt, t.Status.ToString()))
            .ToList();

        var greeting = localNow.Hour switch
        {
            < 12 => $"Good morning, {user.Name}",
            < 18 => $"Good afternoon, {user.Name}",
            _ => $"Good evening, {user.Name}",
        };

        var summary = await SummariseAsync(greeting, localNow, tzi, events, todaysTasks, openTasks.Count, overdue, ct);

        return new DailyBriefing(
            greeting,
            DateOnly.FromDateTime(localMidnight),
            summary,
            events,
            todaysTasks,
            openTasks.Count,
            overdue);
    }

    private async Task<string> SummariseAsync(
        string greeting, DateTime localNow, TimeZoneInfo tzi,
        IReadOnlyList<BriefingEvent> events, IReadOnlyList<BriefingTask> tasks,
        int openCount, int overdue, CancellationToken ct)
    {
        string Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tzi).ToString("h:mm tt");

        var facts = new StringBuilder();
        facts.AppendLine($"Date: {localNow:dddd, MMMM d}");
        facts.AppendLine("All times below are the user's LOCAL time - use them verbatim, do not convert or add a timezone label.");
        facts.AppendLine($"Events today: {events.Count}");
        foreach (var e in events)
            facts.AppendLine($"  - {e.Title} at {Local(e.StartsAt)}{(e.Location is null ? "" : $" ({e.Location})")}");
        facts.AppendLine($"Open tasks: {openCount} ({overdue} overdue)");
        foreach (var t in tasks)
            facts.AppendLine($"  - {t.Title}{(t.DueAt is { } d ? $" due {Local(d)}" : "")} [{t.Priority}]");

        try
        {
            var response = await llm.CompleteAsync(new LlmRequest
            {
                SystemPrompt =
                    "You are a personal assistant giving a spoken morning briefing. " +
                    "2-4 short sentences, natural and encouraging, no bullet points, no markdown. " +
                    $"Start with \"{greeting}.\" Mention the number of events and tasks and the first event or most urgent task.",
                MaxTokens = 250,
                Temperature = 0.5,
                Messages = new[] { LlmMessage.User(facts.ToString()) },
            }, ct);

            if (!string.IsNullOrWhiteSpace(response.Text)) return response.Text!.Trim();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Briefing summary generation failed; using template");
        }

        return $"{greeting}. You have {events.Count} event(s) and {openCount} open task(s) today" +
               (overdue > 0 ? $", {overdue} of them overdue." : ".");
    }
}
