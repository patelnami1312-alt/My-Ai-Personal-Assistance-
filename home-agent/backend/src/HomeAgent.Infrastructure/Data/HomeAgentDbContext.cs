using HomeAgent.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeAgent.Infrastructure.Data;

public class HomeAgentDbContext(DbContextOptions<HomeAgentDbContext> options) : DbContext(options)
{
    public DbSet<Chore> Chores => Set<Chore>();
    public DbSet<MaintenanceItem> MaintenanceItems => Set<MaintenanceItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Chore>(e =>
        {
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DueDate);
        });
        b.Entity<MaintenanceItem>(e => e.HasIndex(x => x.NextDue));
        b.Entity<InventoryItem>(e => e.HasIndex(x => x.Category));

        b.Entity<Conversation>(e =>
            e.HasMany(x => x.Messages).WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<ChatMessage>(e => e.HasIndex(x => new { x.ConversationId, x.CreatedAt }));

        base.OnModelCreating(b);
    }
}
