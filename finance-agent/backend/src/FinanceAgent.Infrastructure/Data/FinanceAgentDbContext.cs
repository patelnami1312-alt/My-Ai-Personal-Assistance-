using FinanceAgent.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceAgent.Infrastructure.Data;

public class FinanceAgentDbContext(DbContextOptions<FinanceAgentDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<FinanceAccount> Accounts => Set<FinanceAccount>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Transaction>(e => e.HasIndex(x => x.Date));
        b.Entity<Transaction>(e => e.HasIndex(x => x.Category));
        b.Entity<Budget>(e => e.HasIndex(x => x.Category).IsUnique());
        b.Entity<Bill>(e => e.HasIndex(x => x.DueDate));

        b.Entity<Conversation>(e =>
            e.HasMany(x => x.Messages).WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<ChatMessage>(e => e.HasIndex(x => new { x.ConversationId, x.CreatedAt }));

        base.OnModelCreating(b);
    }
}
