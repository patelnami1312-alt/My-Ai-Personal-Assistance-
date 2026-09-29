using FitnessAgent.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitnessAgent.Infrastructure.Data;

public class FitnessAgentDbContext(DbContextOptions<FitnessAgentDbContext> options) : DbContext(options)
{
    public DbSet<WorkoutEntry> Workouts => Set<WorkoutEntry>();
    public DbSet<BodyWeightEntry> BodyWeights => Set<BodyWeightEntry>();
    public DbSet<NutritionEntry> Nutrition => Set<NutritionEntry>();
    public DbSet<FitnessGoal> Goals => Set<FitnessGoal>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<WorkoutEntry>(e => e.HasIndex(x => x.Date));
        b.Entity<BodyWeightEntry>(e => e.HasIndex(x => x.Date));
        b.Entity<NutritionEntry>(e => e.HasIndex(x => x.Date));
        b.Entity<FitnessGoal>(e => e.HasIndex(x => x.Status));

        b.Entity<Conversation>(e =>
            e.HasMany(x => x.Messages).WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<ChatMessage>(e => e.HasIndex(x => new { x.ConversationId, x.CreatedAt }));

        base.OnModelCreating(b);
    }
}
