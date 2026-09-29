using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShoppingAgent.Infrastructure.Data;

/// <summary>
/// Used only by `dotnet ef` at design time. Override with the SHOPPING_AGENT_DB env var when scaffolding.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShoppingAgentDbContext>
{
    public ShoppingAgentDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("SHOPPING_AGENT_DB")
                   ?? "Host=localhost;Port=5432;Database=shopping_agent;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<ShoppingAgentDbContext>().UseNpgsql(conn).Options;
        return new ShoppingAgentDbContext(options);
    }
}
