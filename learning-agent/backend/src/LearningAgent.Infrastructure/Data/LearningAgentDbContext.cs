using LearningAgent.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearningAgent.Infrastructure.Data;

public class LearningAgentDbContext(DbContextOptions<LearningAgentDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<LearningNote> Notes => Set<LearningNote>();
    public DbSet<LearningGoal> Goals => Set<LearningGoal>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Course>(e => e.HasIndex(x => x.Status));
        b.Entity<StudySession>(e => e.HasIndex(x => x.Date));
        b.Entity<LearningGoal>(e => e.HasIndex(x => x.Status));

        b.Entity<Conversation>(e =>
            e.HasMany(x => x.Messages).WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<ChatMessage>(e => e.HasIndex(x => new { x.ConversationId, x.CreatedAt }));

        base.OnModelCreating(b);
    }
}
