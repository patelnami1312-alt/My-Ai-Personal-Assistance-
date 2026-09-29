using CodingAgent.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Infrastructure.Data;

public class CodingAgentDbContext(DbContextOptions<CodingAgentDbContext> options) : DbContext(options)
{
    public DbSet<CodeProject> Projects => Set<CodeProject>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<CodeProject>(e => e.HasIndex(x => x.RootPath).IsUnique());
        base.OnModelCreating(b);
    }
}
