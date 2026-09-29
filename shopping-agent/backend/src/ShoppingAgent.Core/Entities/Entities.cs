namespace ShoppingAgent.Core.Entities;

public class ShoppingItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public string? Category { get; set; }
    public string? Store { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public bool Purchased { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PurchasedAt { get; set; }
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
