namespace Assistant.Core.Dtos;

public record ChatRequest(Guid? ConversationId, string Message);

public record ToolTraceEntry(string Tool, string Arguments, string Result, bool IsError);

public record ChatTurnResult(
    Guid ConversationId,
    string Reply,
    IReadOnlyList<ToolTraceEntry> ToolTrace,
    int InputTokens,
    int OutputTokens);

public record BriefingTask(Guid Id, string Title, string Priority, DateTime? DueAt, string Status);
public record BriefingEvent(Guid Id, string Title, DateTime StartsAt, DateTime EndsAt, string? Location, bool AllDay);

public record DailyBriefing(
    string Greeting,
    DateOnly Date,
    string Summary,
    IReadOnlyList<BriefingEvent> Events,
    IReadOnlyList<BriefingTask> Tasks,
    int OpenTaskCount,
    int OverdueTaskCount);

