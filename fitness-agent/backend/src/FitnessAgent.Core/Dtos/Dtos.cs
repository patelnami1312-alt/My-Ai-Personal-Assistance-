namespace FitnessAgent.Core.Dtos;

public record ChatRequest(Guid? ConversationId, string Message);
public record ToolTraceEntry(string Tool, string Arguments, string Result, bool IsError);
public record ChatTurnResult(Guid ConversationId, string Reply, IReadOnlyList<ToolTraceEntry> ToolTrace, int InputTokens, int OutputTokens);

public record ConversationSummary(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);
public record ChatMessageDto(Guid Id, string Role, string Content, string? ToolTrace, DateTime CreatedAt);
public record ConversationDetail(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<ChatMessageDto> Messages);

public record WorkoutInput(string Exercise, int? Sets, int? Reps, double? WeightKg, int? DurationMinutes, string? Notes, DateTime? Date);
public record BodyWeightInput(double WeightKg, string? Notes, DateTime? Date);
public record NutritionInput(string Description, int? Calories, double? ProteinG, double? CarbsG, double? FatG, DateTime? Date);
public record GoalInput(string Type, string Description, double? TargetValue, DateTime? TargetDate, string? Status);

public record DashboardSummary(
    int WorkoutsThisWeek,
    double? LatestWeightKg,
    double? WeightChange30dKg,
    int ActiveGoals,
    IReadOnlyList<WorkoutSummaryRow> RecentWorkouts);

public record WorkoutSummaryRow(Guid Id, DateTime Date, string Exercise, int? Sets, int? Reps, double? WeightKg);
