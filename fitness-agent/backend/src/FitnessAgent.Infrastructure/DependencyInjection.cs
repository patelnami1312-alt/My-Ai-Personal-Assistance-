using FitnessAgent.Core.Abstractions;
using FitnessAgent.Infrastructure.Data;
using FitnessAgent.Infrastructure.Llm;
using FitnessAgent.Infrastructure.Orchestration;
using FitnessAgent.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFitnessAgentInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<FitnessAgentDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("FitnessAgent")
                        ?? throw new InvalidOperationException("ConnectionStrings:FitnessAgent is not configured.")));

        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        services.AddSingleton<ILlmProvider, OpenAiLlmProvider>();

        services.AddScoped<ITool, LogWorkoutTool>();
        services.AddScoped<ITool, GetWorkoutsTool>();
        services.AddScoped<ITool, DeleteWorkoutTool>();

        services.AddScoped<ITool, LogBodyWeightTool>();
        services.AddScoped<ITool, GetBodyWeightHistoryTool>();
        services.AddScoped<ITool, DeleteBodyWeightEntryTool>();

        services.AddScoped<ITool, LogMealTool>();
        services.AddScoped<ITool, GetMealsTool>();
        services.AddScoped<ITool, DeleteMealTool>();

        services.AddScoped<ITool, SetGoalTool>();
        services.AddScoped<ITool, GetGoalsTool>();
        services.AddScoped<ITool, UpdateGoalTool>();
        services.AddScoped<ITool, DeleteGoalTool>();

        services.AddScoped<ITool, DateTimeTool>();

        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IOrchestrator, OrchestratorService>();

        return services;
    }
}
