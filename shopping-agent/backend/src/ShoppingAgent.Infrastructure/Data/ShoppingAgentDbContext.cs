using ShoppingAgent.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShoppingAgent.Infrastructure.Data;

public class ShoppingAgentDbContext(DbContextOptions<ShoppingAgentDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingItem> Items => Set<ShoppingItem>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ShoppingItem>(e =>
        {
            e.HasIndex(x => x.Purchased);
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.EstimatedPrice).HasColumnType("numeric(10,2)");
        });

        b.Entity<Conversation>(e =>
            e.HasMany(x => x.Messages).WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<ChatMessage>(e => e.HasIndex(x => new { x.ConversationId, x.CreatedAt }));

        base.OnModelCreating(b);
    }
}
