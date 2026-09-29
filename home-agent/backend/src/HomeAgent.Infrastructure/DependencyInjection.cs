using HomeAgent.Core.Abstractions;
using HomeAgent.Infrastructure.Data;
using HomeAgent.Infrastructure.Llm;
using HomeAgent.Infrastructure.Orchestration;
using HomeAgent.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHomeAgentInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<HomeAgentDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("HomeAgent")
                        ?? throw new InvalidOperationException("ConnectionStrings:HomeAgent is not configured.")));

        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        services.AddSingleton<ILlmProvider, OpenAiLlmProvider>();

        services.AddScoped<ITool, AddChoreTool>();
        services.AddScoped<ITool, GetChoresTool>();
        services.AddScoped<ITool, CompleteChoreTool>();
        services.AddScoped<ITool, DeleteChoreTool>();

        services.AddScoped<ITool, AddMaintenanceItemTool>();
        services.AddScoped<ITool, GetMaintenanceItemsTool>();
        services.AddScoped<ITool, MarkMaintenanceDoneTool>();
        services.AddScoped<ITool, DeleteMaintenanceItemTool>();

        services.AddScoped<ITool, AddInventoryItemTool>();
        services.AddScoped<ITool, GetInventoryTool>();
        services.AddScoped<ITool, UpdateInventoryQuantityTool>();
        services.AddScoped<ITool, DeleteInventoryItemTool>();

        services.AddScoped<ITool, DateTimeTool>();

        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IOrchestrator, OrchestratorService>();

        return services;
    }
}
