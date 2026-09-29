namespace HomeAgent.Core.Entities;

public enum ChoreStatus { Todo = 0, Done = 1 }
public enum RecurrenceFrequency { OneTime = 0, Daily = 1, Weekly = 2, Monthly = 3 }

public class Chore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public bool Recurring { get; set; }
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.OneTime;
    public ChoreStatus Status { get; set; } = ChoreStatus.Todo;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public class MaintenanceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public DateTime? LastDone { get; set; }
    public int? IntervalDays { get; set; }
    public DateTime? NextDue { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public int Quantity { get; set; }
    public int LowThreshold { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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
