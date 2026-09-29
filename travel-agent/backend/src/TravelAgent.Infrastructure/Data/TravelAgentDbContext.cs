using TravelAgent.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace TravelAgent.Infrastructure.Data;

public class TravelAgentDbContext(DbContextOptions<TravelAgentDbContext> options) : DbContext(options)
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<PackingItem> PackingItems => Set<PackingItem>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Trip>(e => e.HasIndex(x => x.StartDate));

        b.Entity<ItineraryItem>(e =>
        {
            e.HasIndex(x => x.TripId);
            e.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PackingItem>(e =>
        {
            e.HasIndex(x => x.TripId);
            e.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Conversation>(e =>
            e.HasMany(x => x.Messages).WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<ChatMessage>(e => e.HasIndex(x => new { x.ConversationId, x.CreatedAt }));

        base.OnModelCreating(b);
    }
}
