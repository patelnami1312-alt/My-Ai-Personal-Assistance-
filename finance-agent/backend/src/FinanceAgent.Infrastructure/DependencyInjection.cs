using FinanceAgent.Core.Abstractions;
using FinanceAgent.Infrastructure.Data;
using FinanceAgent.Infrastructure.Llm;
using FinanceAgent.Infrastructure.Orchestration;
using FinanceAgent.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFinanceAgentInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<FinanceAgentDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("FinanceAgent")
                        ?? throw new InvalidOperationException("ConnectionStrings:FinanceAgent is not configured.")));

        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        services.AddSingleton<ILlmProvider, OpenAiLlmProvider>();

        services.AddScoped<ITool, LogTransactionTool>();
        services.AddScoped<ITool, GetTransactionsTool>();
        services.AddScoped<ITool, DeleteTransactionTool>();
        services.AddScoped<ITool, GetSpendingSummaryTool>();

        services.AddScoped<ITool, AddAccountTool>();
        services.AddScoped<ITool, GetAccountsTool>();
        services.AddScoped<ITool, UpdateAccountBalanceTool>();

        services.AddScoped<ITool, SetBudgetTool>();
        services.AddScoped<ITool, GetBudgetsTool>();

        services.AddScoped<ITool, AddBillTool>();
        services.AddScoped<ITool, GetBillsTool>();
        services.AddScoped<ITool, MarkBillPaidTool>();
        services.AddScoped<ITool, DeleteBillTool>();

        services.AddScoped<ITool, DateTimeTool>();

        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IOrchestrator, OrchestratorService>();

        return services;
    }
}
