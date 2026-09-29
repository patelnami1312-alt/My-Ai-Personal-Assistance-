using TravelAgent.Core.Abstractions;
using TravelAgent.Infrastructure.Data;
using TravelAgent.Infrastructure.Llm;
using TravelAgent.Infrastructure.Orchestration;
using TravelAgent.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TravelAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTravelAgentInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<TravelAgentDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("TravelAgent")
                        ?? throw new InvalidOperationException("ConnectionStrings:TravelAgent is not configured.")));

        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        services.AddSingleton<ILlmProvider, OpenAiLlmProvider>();

        services.AddScoped<ITool, CreateTripTool>();
        services.AddScoped<ITool, GetTripsTool>();
        services.AddScoped<ITool, UpdateTripTool>();
        services.AddScoped<ITool, DeleteTripTool>();

        services.AddScoped<ITool, AddItineraryItemTool>();
        services.AddScoped<ITool, GetItineraryTool>();
        services.AddScoped<ITool, DeleteItineraryItemTool>();

        services.AddScoped<ITool, AddPackingItemTool>();
        services.AddScoped<ITool, GetPackingListTool>();
        services.AddScoped<ITool, CheckPackingItemTool>();
        services.AddScoped<ITool, DeletePackingItemTool>();

        services.AddScoped<ITool, DateTimeTool>();

        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IOrchestrator, OrchestratorService>();

        return services;
    }
}
