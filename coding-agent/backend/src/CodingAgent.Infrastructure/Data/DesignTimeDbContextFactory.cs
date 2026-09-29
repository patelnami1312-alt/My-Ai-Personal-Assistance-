using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodingAgent.Infrastructure.Data;

/// <summary>
/// Used only by `dotnet ef` at design time so migrations don't need to boot the API host.
/// Override the connection string with the CODING_AGENT_DB env var when scaffolding.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CodingAgentDbContext>
{
    public CodingAgentDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("CODING_AGENT_DB")
                   ?? "Host=localhost;Port=5432;Database=coding_agent;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<CodingAgentDbContext>()
            .UseNpgsql(conn)
            .Options;
        return new CodingAgentDbContext(options);
    }
}
