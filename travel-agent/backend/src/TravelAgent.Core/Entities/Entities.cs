namespace TravelAgent.Core.Entities;

public enum TripStatus { Planned = 0, Ongoing = 1, Completed = 2 }

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Destination { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Planned;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ItineraryItemType { Flight = 0, Hotel = 1, Activity = 2, Transport = 3, Other = 4 }

public class ItineraryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public string Title { get; set; } = "";
    public ItineraryItemType Type { get; set; } = ItineraryItemType.Other;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PackingItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public bool Packed { get; set; }
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
