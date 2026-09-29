using LearningAgent.Core.Dtos;
using LearningAgent.Core.Entities;
using LearningAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningAgent.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(LearningAgentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var studyMinutesThisWeek = await db.StudySessions
            .Where(s => s.Date >= weekAgo)
            .SumAsync(s => (int?)s.DurationMinutes, ct) ?? 0;

        var activeCoursesCount = await db.Courses.CountAsync(c => c.Status == CourseStatus.Active, ct);
        var activeGoalsCount = await db.Goals.CountAsync(g => g.Status == LearningGoalStatus.Active, ct);

        var recent = await db.StudySessions
            .OrderByDescending(s => s.Date)
            .Take(5)
            .Select(s => new StudySessionSummaryRow(s.Id, s.Topic, s.DurationMinutes, s.Date))
            .ToListAsync(ct);

        var summary = new DashboardSummary(studyMinutesThisWeek, activeCoursesCount, activeGoalsCount, recent);

        return Ok(summary);
    }
}
