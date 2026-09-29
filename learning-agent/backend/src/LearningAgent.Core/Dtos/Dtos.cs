namespace LearningAgent.Core.Dtos;

public record ChatRequest(Guid? ConversationId, string Message);
public record ToolTraceEntry(string Tool, string Arguments, string Result, bool IsError);
public record ChatTurnResult(Guid ConversationId, string Reply, IReadOnlyList<ToolTraceEntry> ToolTrace, int InputTokens, int OutputTokens);

public record ConversationSummary(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);
public record ChatMessageDto(Guid Id, string Role, string Content, string? ToolTrace, DateTime CreatedAt);
public record ConversationDetail(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<ChatMessageDto> Messages);

public record CourseInput(string Title, string? Subject, string? Notes, string? Status, DateTime? StartedAt);
public record StudySessionInput(string Topic, int DurationMinutes, Guid? CourseId, DateTime? Date, string? Notes);
public record NoteInput(string Title, string Content, List<string>? Tags);
public record GoalInput(string Description, DateTime? TargetDate, string? Status);

public record DashboardSummary(
    int StudyMinutesThisWeek,
    int ActiveCoursesCount,
    int ActiveGoalsCount,
    IReadOnlyList<StudySessionSummaryRow> RecentSessions);

public record StudySessionSummaryRow(Guid Id, string Topic, int DurationMinutes, DateTime Date);
