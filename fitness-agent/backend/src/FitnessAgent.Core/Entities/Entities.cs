namespace FitnessAgent.Core.Entities;

public class WorkoutEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Exercise { get; set; } = "";
    public int? Sets { get; set; }
    public int? Reps { get; set; }
    public double? WeightKg { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BodyWeightEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public double WeightKg { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class NutritionEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = "";
    public int? Calories { get; set; }
    public double? ProteinG { get; set; }
    public double? CarbsG { get; set; }
    public double? FatG { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum GoalType { WeightTarget = 0, WorkoutFrequency = 1, Custom = 2 }
public enum GoalStatus { Active = 0, Achieved = 1, Abandoned = 2 }

public class FitnessGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public GoalType Type { get; set; } = GoalType.Custom;
    public string Description { get; set; } = "";
    /// <summary>Free-form target - e.g. "70" (kg) for WeightTarget, "4" (sessions/week) for WorkoutFrequency.</summary>
    public double? TargetValue { get; set; }
    public DateTime? TargetDate { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
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
