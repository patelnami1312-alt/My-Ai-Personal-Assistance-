using CodingAgent.Infrastructure.Data;
using CodingAgent.Infrastructure.Workspace;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodingAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCodingAgentInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<CodingAgentDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("CodingAgent")
                        ?? throw new InvalidOperationException("ConnectionStrings:CodingAgent is not configured.")));

        services.AddSingleton<IWorkspaceService, WorkspaceService>();

        return services;
    }
}
