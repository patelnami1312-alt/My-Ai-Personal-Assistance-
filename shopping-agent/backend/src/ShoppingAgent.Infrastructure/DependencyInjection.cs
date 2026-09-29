using ShoppingAgent.Core.Abstractions;
using ShoppingAgent.Infrastructure.Data;
using ShoppingAgent.Infrastructure.Llm;
using ShoppingAgent.Infrastructure.Orchestration;
using ShoppingAgent.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ShoppingAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShoppingAgentInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ShoppingAgentDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("ShoppingAgent")
                        ?? throw new InvalidOperationException("ConnectionStrings:ShoppingAgent is not configured.")));

        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        services.AddSingleton<ILlmProvider, OpenAiLlmProvider>();

        services.AddScoped<ITool, AddItemTool>();
        services.AddScoped<ITool, GetShoppingListTool>();
        services.AddScoped<ITool, MarkPurchasedTool>();
        services.AddScoped<ITool, UpdateItemTool>();
        services.AddScoped<ITool, DeleteItemTool>();
        services.AddScoped<ITool, GetPurchaseHistoryTool>();

        services.AddScoped<ITool, DateTimeTool>();

        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IOrchestrator, OrchestratorService>();

        return services;
    }
}
