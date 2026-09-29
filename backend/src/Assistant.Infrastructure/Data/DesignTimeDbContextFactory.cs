using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Assistant.Infrastructure.Data;

/// <summary>
/// Used only by `dotnet ef` at design time so migrations don't need to boot the API host.
/// Override the connection string with the ASSISTANT_DB env var when scaffolding.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AssistantDbContext>
{
    public AssistantDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ASSISTANT_DB")
                   ?? "Host=localhost;Port=5432;Database=ai_personal_assistant;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<AssistantDbContext>()
            .UseNpgsql(conn)
            .Options;
        return new AssistantDbContext(options);
    }
}
