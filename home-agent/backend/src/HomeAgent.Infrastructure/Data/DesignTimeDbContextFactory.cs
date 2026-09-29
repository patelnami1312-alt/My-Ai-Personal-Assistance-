using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeAgent.Infrastructure.Data;

/// <summary>
/// Used only by `dotnet ef` at design time. Override with the HOME_AGENT_DB env var when scaffolding.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HomeAgentDbContext>
{
    public HomeAgentDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("HOME_AGENT_DB")
                   ?? "Host=localhost;Port=5432;Database=home_agent;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<HomeAgentDbContext>().UseNpgsql(conn).Options;
        return new HomeAgentDbContext(options);
    }
}
