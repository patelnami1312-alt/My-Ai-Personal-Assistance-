namespace FinanceAgent.Core.Dtos;

public record ChatRequest(Guid? ConversationId, string Message);
public record ToolTraceEntry(string Tool, string Arguments, string Result, bool IsError);
public record ChatTurnResult(Guid ConversationId, string Reply, IReadOnlyList<ToolTraceEntry> ToolTrace, int InputTokens, int OutputTokens);

public record ConversationSummary(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);
public record ChatMessageDto(Guid Id, string Role, string Content, string? ToolTrace, DateTime CreatedAt);
public record ConversationDetail(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<ChatMessageDto> Messages);

public record TransactionInput(string Description, decimal Amount, string Type, string Category, string? Notes, DateTime? Date);
public record AccountInput(string Name, string Type, decimal Balance, string? Notes);
public record BudgetInput(string Category, decimal MonthlyLimit);
public record BillInput(string Name, decimal Amount, DateTime DueDate, bool Recurring, string? Frequency, string? Notes, bool Paid = false);

public record UpcomingBillRow(Guid Id, string Name, decimal Amount, DateTime DueDate);
public record BudgetStatusRow(string Category, decimal Limit, decimal Spent);

public record DashboardSummary(
    decimal TotalIncomeThisMonth,
    decimal TotalExpenseThisMonth,
    decimal NetThisMonth,
    decimal TotalAccountBalance,
    IReadOnlyList<UpcomingBillRow> UpcomingBills,
    IReadOnlyList<BudgetStatusRow> BudgetStatus);
