using Assistant.Core.Abstractions;
using Assistant.Infrastructure.Agents.Personal;
using Assistant.Infrastructure.Data;
using Assistant.Infrastructure.Llm;
using Assistant.Infrastructure.Orchestration;
using Assistant.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Assistant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAssistantInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ---- shared plumbing ----
        services.AddDbContext<AssistantDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("Assistant")
                        ?? throw new InvalidOperationException("ConnectionStrings:Assistant is not configured.")));

        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        var provider = config.GetSection(LlmOptions.SectionName)["Provider"]?.Trim().ToLowerInvariant();
        switch (provider)
        {
            case "openai":
                services.AddSingleton<ILlmProvider, OpenAiLlmProvider>();
                break;
            case "anthropic":
            case null or "":
                services.AddSingleton<ILlmProvider, AnthropicLlmProvider>();
                break;
            default:
                throw new InvalidOperationException($"Unknown Llm:Provider '{provider}'. Use 'openai' or 'anthropic'.");
        }

        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IOrchestrator, OrchestratorService>();

        // ---- agents (each owns its own tools/services) ----
        services.AddPersonalAgent();

        return services;
    }

    /// <summary>Personal agent: tasks, calendar, notes, long-term memory, daily briefing.</summary>
    private static void AddPersonalAgent(this IServiceCollection services)
    {
        services.AddScoped<ITool, CreateTaskTool>();
        services.AddScoped<ITool, GetTasksTool>();
        services.AddScoped<ITool, UpdateTaskTool>();
        services.AddScoped<ITool, CompleteTaskTool>();
        services.AddScoped<ITool, DeleteTaskTool>();

        services.AddScoped<ITool, CreateNoteTool>();
        services.AddScoped<ITool, SearchNotesTool>();
        services.AddScoped<ITool, DeleteNoteTool>();

        services.AddScoped<ITool, CreateEventTool>();
        services.AddScoped<ITool, GetEventsTool>();
        services.AddScoped<ITool, UpdateEventTool>();
        services.AddScoped<ITool, DeleteEventTool>();
        services.AddScoped<ITool, FindFreeTimeTool>();

        services.AddScoped<ITool, RecallTool>();
        services.AddScoped<ITool, ForgetTool>();
        services.AddScoped<ITool, UpdateProfileTool>();

        services.AddScoped<ITool, DateTimeTool>();
        services.AddScoped<ITool, CalculatorTool>();
        services.AddScoped<ITool, WebSearchTool>();

        services.AddScoped<IMemoryExtractor, MemoryExtractionService>();
        services.AddScoped<IBriefingService, BriefingService>();
    }
}
