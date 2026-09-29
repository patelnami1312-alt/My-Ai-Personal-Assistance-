using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitnessAgent.Infrastructure.Data;

/// <summary>
/// Used only by `dotnet ef` at design time. Override with the FITNESS_AGENT_DB env var when scaffolding.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FitnessAgentDbContext>
{
    public FitnessAgentDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("FITNESS_AGENT_DB")
                   ?? "Host=localhost;Port=5432;Database=fitness_agent;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<FitnessAgentDbContext>().UseNpgsql(conn).Options;
        return new FitnessAgentDbContext(options);
    }
}
