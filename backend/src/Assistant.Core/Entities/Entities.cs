namespace Assistant.Core.Entities;

/// <summary>
/// A single human user of the assistant. For now the app runs single-user, but every
/// row is scoped by UserId so multi-user / auth can be layered on later without a migration.
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string TimeZone { get; set; } = "UTC";
    public string Language { get; set; } = "en";
    /// <summary>Free-form preferences the user has stated ("prefers concise answers", "24h clock").</summary>
    public List<string> Preferences { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum TaskState { Todo = 0, InProgress = 1, Done = 2 }

public enum TaskPriority { Low = 0, Medium = 1, High = 2 }

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueAt { get; set; }
    public TaskState Status { get; set; } = TaskState.Todo;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public class Note
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CalendarEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool AllDay { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum MemoryCategory { Fact = 0, Preference = 1, Goal = 2, Relationship = 3, Event = 4 }

/// <summary>
/// A durable fact the assistant chose to remember about the user. Retrieval is naive for now
/// (load-all + keyword match); a pgvector Embedding column is the planned upgrade.
/// </summary>
public class MemoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Content { get; set; } = "";
    public MemoryCategory Category { get; set; } = MemoryCategory.Fact;
    public List<string> Keywords { get; set; } = new();
    /// <summary>0-1 model confidence that this is worth keeping long term.</summary>
    public double Confidence { get; set; } = 0.7;
    public string? SourceConversationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = "New conversation";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Messages { get; set; } = new();
}

public enum ChatRole { System = 0, User = 1, Assistant = 2, Tool = 3 }

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = "";
    /// <summary>JSON snapshot of any tool calls/results attached to this turn, for transcript display.</summary>
    public string? ToolTrace { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
