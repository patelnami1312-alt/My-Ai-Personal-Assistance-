namespace ShoppingAgent.Core.Dtos;

public record ChatRequest(Guid? ConversationId, string Message);
public record ToolTraceEntry(string Tool, string Arguments, string Result, bool IsError);
public record ChatTurnResult(Guid ConversationId, string Reply, IReadOnlyList<ToolTraceEntry> ToolTrace, int InputTokens, int OutputTokens);

public record ConversationSummary(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);
public record ChatMessageDto(Guid Id, string Role, string Content, string? ToolTrace, DateTime CreatedAt);
public record ConversationDetail(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<ChatMessageDto> Messages);

public record ShoppingItemInput(string Name, int? Quantity, string? Category, string? Store, decimal? EstimatedPrice, string? Notes, bool? Purchased);

public record DashboardSummary(
    int ActiveItemsCount,
    int PurchasedThisWeekCount,
    decimal EstimatedTotal,
    IReadOnlyList<RecentItemRow> RecentItems);

public record RecentItemRow(Guid Id, string Name, int Quantity, string? Category, bool Purchased);
