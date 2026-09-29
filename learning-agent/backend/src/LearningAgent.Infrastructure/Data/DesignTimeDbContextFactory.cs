using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearningAgent.Infrastructure.Data;

/// <summary>
/// Used only by `dotnet ef` at design time. Override with the LEARNING_AGENT_DB env var when scaffolding.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LearningAgentDbContext>
{
    public LearningAgentDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("LEARNING_AGENT_DB")
                   ?? "Host=localhost;Port=5432;Database=learning_agent;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<LearningAgentDbContext>().UseNpgsql(conn).Options;
        return new LearningAgentDbContext(options);
    }
}
