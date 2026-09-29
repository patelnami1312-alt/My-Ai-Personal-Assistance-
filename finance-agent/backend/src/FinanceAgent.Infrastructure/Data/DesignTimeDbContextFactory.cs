using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinanceAgent.Infrastructure.Data;

/// <summary>
/// Used only by `dotnet ef` at design time. Override with the FINANCE_AGENT_DB env var when scaffolding.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinanceAgentDbContext>
{
    public FinanceAgentDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("FINANCE_AGENT_DB")
                   ?? "Host=localhost;Port=5432;Database=finance_agent;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<FinanceAgentDbContext>().UseNpgsql(conn).Options;
        return new FinanceAgentDbContext(options);
    }
}
