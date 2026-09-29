namespace LearningAgent.Core.Entities;

public enum CourseStatus { Active = 0, Completed = 1, Paused = 2 }

public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string? Subject { get; set; }
    public CourseStatus Status { get; set; } = CourseStatus.Active;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StudySession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CourseId { get; set; }
    public string Topic { get; set; } = "";
    public int DurationMinutes { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LearningNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum LearningGoalStatus { Active = 0, Achieved = 1, Abandoned = 2 }

public class LearningGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = "";
    public DateTime? TargetDate { get; set; }
    public LearningGoalStatus Status { get; set; } = LearningGoalStatus.Active;
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
