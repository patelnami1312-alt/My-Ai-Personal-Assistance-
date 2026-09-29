using HomeAgent.Core.Abstractions;
using HomeAgent.Core.Llm;

namespace HomeAgent.Infrastructure.Tools;

/// <summary>Collects every <see cref="ITool"/> registered in DI and exposes them to the orchestrator.</summary>
public class ToolRegistry(IEnumerable<ITool> tools) : IToolRegistry
{
    private readonly Dictionary<string, ITool> _byName =
        tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ITool> All => _byName.Values.ToList();

    public ITool? Find(string name) => _byName.GetValueOrDefault(name);

    public IReadOnlyList<LlmToolDefinition> ToLlmDefinitions() =>
        _byName.Values
            .OrderBy(t => t.Name)
            .Select(t => new LlmToolDefinition(t.Name, t.Description, t.ParametersSchema))
            .ToList();
}
