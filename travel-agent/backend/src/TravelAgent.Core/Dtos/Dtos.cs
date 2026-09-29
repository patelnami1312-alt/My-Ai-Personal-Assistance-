namespace TravelAgent.Core.Dtos;

public record ChatRequest(Guid? ConversationId, string Message);
public record ToolTraceEntry(string Tool, string Arguments, string Result, bool IsError);
public record ChatTurnResult(Guid ConversationId, string Reply, IReadOnlyList<ToolTraceEntry> ToolTrace, int InputTokens, int OutputTokens);

public record ConversationSummary(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);
public record ChatMessageDto(Guid Id, string Role, string Content, string? ToolTrace, DateTime CreatedAt);
public record ConversationDetail(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<ChatMessageDto> Messages);

public record TripInput(string Destination, DateTime StartDate, DateTime EndDate, string? Status, string? Notes);
public record ItineraryItemInput(Guid TripId, string Title, string Type, DateTime StartsAt, DateTime? EndsAt, string? Location, string? Notes);
public record PackingItemInput(Guid TripId, string Name, string? Category, bool? Packed);

public record DashboardSummary(
    int TotalTrips,
    UpcomingTripRow? UpcomingTrip,
    PackingProgressRow? PackingProgress);

public record UpcomingTripRow(Guid Id, string Destination, DateTime StartDate, DateTime EndDate);
public record PackingProgressRow(int Packed, int Total);
