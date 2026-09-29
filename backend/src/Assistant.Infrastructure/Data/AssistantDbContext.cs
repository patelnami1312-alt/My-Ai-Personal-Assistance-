using Assistant.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Infrastructure.Data;

public class AssistantDbContext(DbContextOptions<AssistantDbContext> options) : DbContext(options)
{
    /// <summary>Fixed id for the single local user until auth lands.</summary>
    public static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Idempotently ensures the default user row exists. Called on startup.</summary>
    public async Task EnsureSeedAsync(CancellationToken ct = default)
    {
        if (!await Users.AnyAsync(u => u.Id == DefaultUserId, ct))
        {
            Users.Add(new UserProfile
            {
                Id = DefaultUserId,
                Name = "there",
                TimeZone = "UTC",
                Language = "en",
            });
            await SaveChangesAsync(ct);
        }
    }

    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();
    public DbSet<MemoryItem> Memories => Set<MemoryItem>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // The default user is seeded at startup (see EnsureSeedAsync) rather than via HasData,
        // which trips PendingModelChangesWarning on array-typed seed properties.

        b.Entity<TaskItem>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.Status });
            e.HasIndex(x => new { x.UserId, x.DueAt });
        });

        b.Entity<Note>(e => e.HasIndex(x => x.UserId));

        b.Entity<CalendarEvent>(e => e.HasIndex(x => new { x.UserId, x.StartsAt }));

        b.Entity<MemoryItem>(e => e.HasIndex(x => x.UserId));

        b.Entity<Conversation>(e =>
        {
            e.HasIndex(x => x.UserId);
            e.HasMany(x => x.Messages).WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ChatMessage>(e => e.HasIndex(x => new { x.ConversationId, x.CreatedAt }));

        base.OnModelCreating(b);
    }
}
