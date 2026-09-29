using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TravelAgent.Infrastructure.Data;

/// <summary>
/// Used only by `dotnet ef` at design time. Override with the TRAVEL_AGENT_DB env var when scaffolding.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TravelAgentDbContext>
{
    public TravelAgentDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("TRAVEL_AGENT_DB")
                   ?? "Host=localhost;Port=5432;Database=travel_agent;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<TravelAgentDbContext>().UseNpgsql(conn).Options;
        return new TravelAgentDbContext(options);
    }
}
