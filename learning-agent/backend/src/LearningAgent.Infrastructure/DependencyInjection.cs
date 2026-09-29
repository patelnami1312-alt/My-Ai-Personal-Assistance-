using LearningAgent.Core.Abstractions;
using LearningAgent.Infrastructure.Data;
using LearningAgent.Infrastructure.Llm;
using LearningAgent.Infrastructure.Orchestration;
using LearningAgent.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearningAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLearningAgentInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<LearningAgentDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("LearningAgent")
                        ?? throw new InvalidOperationException("ConnectionStrings:LearningAgent is not configured.")));

        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        services.AddSingleton<ILlmProvider, OpenAiLlmProvider>();

        services.AddScoped<ITool, AddCourseTool>();
        services.AddScoped<ITool, GetCoursesTool>();
        services.AddScoped<ITool, UpdateCourseStatusTool>();

        services.AddScoped<ITool, LogStudySessionTool>();
        services.AddScoped<ITool, GetStudySessionsTool>();
        services.AddScoped<ITool, GetStudySummaryTool>();

        services.AddScoped<ITool, AddNoteTool>();
        services.AddScoped<ITool, SearchNotesTool>();
        services.AddScoped<ITool, DeleteNoteTool>();

        services.AddScoped<ITool, SetLearningGoalTool>();
        services.AddScoped<ITool, GetLearningGoalsTool>();
        services.AddScoped<ITool, UpdateLearningGoalTool>();
        services.AddScoped<ITool, DeleteLearningGoalTool>();

        services.AddScoped<ITool, DateTimeTool>();

        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IOrchestrator, OrchestratorService>();

        return services;
    }
}
