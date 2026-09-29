namespace FinanceAgent.Core.Entities;

public enum TransactionType { Income = 0, Expense = 1 }

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Category { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AccountType { Checking = 0, Savings = 1, Credit = 2, Cash = 3, Investment = 4 }

public class FinanceAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Budget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Category { get; set; } = "";
    public decimal MonthlyLimit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum BillFrequency { OneTime = 0, Weekly = 1, Monthly = 2, Yearly = 3 }

public class Bill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public bool Recurring { get; set; }
    public BillFrequency Frequency { get; set; } = BillFrequency.OneTime;
    public bool Paid { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "New conversation";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Messages { get; set; } = new();
}

public enum ChatRole { User = 0, Assistant = 1 }

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = "";
    public string? ToolTrace { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
