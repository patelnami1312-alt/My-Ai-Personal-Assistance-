using System.Text.Json.Nodes;

namespace LearningAgent.Infrastructure.Tools;

/// <summary>Tiny fluent builder for the JSON-Schema objects tool definitions need.</summary>
public sealed class Schema
{
    private readonly JsonObject _props = new();
    private readonly JsonArray _required = new();

    public static Schema Object() => new();

    public Schema Str(string name, string description, bool required = false, string[]? enumValues = null)
        => Add(name, "string", description, required, enumValues);

    public Schema Int(string name, string description, bool required = false)
        => Add(name, "integer", description, required);

    public Schema Num(string name, string description, bool required = false)
        => Add(name, "number", description, required);

    public Schema Bool(string name, string description, bool required = false)
        => Add(name, "boolean", description, required);

    private Schema Add(string name, string type, string description, bool required, string[]? enumValues = null)
    {
        var p = new JsonObject { ["type"] = type, ["description"] = description };
        if (enumValues is { Length: > 0 })
            p["enum"] = new JsonArray(enumValues.Select(v => (JsonNode)v!).ToArray());
        _props[name] = p;
        if (required) _required.Add(name);
        return this;
    }

    public JsonObject Build() => new()
    {
        ["type"] = "object",
        ["properties"] = _props,
        ["required"] = _required,
    };
}
