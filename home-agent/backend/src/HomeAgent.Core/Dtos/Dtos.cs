namespace HomeAgent.Core.Dtos;

public record ChatRequest(Guid? ConversationId, string Message);
public record ToolTraceEntry(string Tool, string Arguments, string Result, bool IsError);
public record ChatTurnResult(Guid ConversationId, string Reply, IReadOnlyList<ToolTraceEntry> ToolTrace, int InputTokens, int OutputTokens);

public record ConversationSummary(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);
public record ChatMessageDto(Guid Id, string Role, string Content, string? ToolTrace, DateTime CreatedAt);
public record ConversationDetail(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<ChatMessageDto> Messages);

public record ChoreInput(string Title, string? AssignedTo, DateTime? DueDate, bool? Recurring, string? Frequency, string? Notes, string? Status);
public record MaintenanceInput(string Title, int? IntervalDays, DateTime? LastDone, string? Notes);
public record InventoryInput(string Name, string? Category, int Quantity, int? LowThreshold, string? Notes);

public record DashboardSummary(
    int ChoresDueTodayCount,
    int OverdueChoresCount,
    int LowInventoryCount,
    IReadOnlyList<UpcomingMaintenanceRow> UpcomingMaintenance,
    IReadOnlyList<RecentChoreRow> RecentChores);

public record UpcomingMaintenanceRow(Guid Id, string Title, DateTime? NextDue);
public record RecentChoreRow(Guid Id, string Title, string Status, DateTime? DueDate);
