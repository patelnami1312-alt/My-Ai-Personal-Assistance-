using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TravelAgent.Infrastructure.Tools;

public static class ArgsExtensions
{
    public static string? GetString(this JsonObject args, string key)
    {
        if (!args.TryGetPropertyValue(key, out var n) || n is null) return null;
        try { return n.GetValueKind() == JsonValueKind.String ? n.GetValue<string>() : n.ToJsonString(); }
        catch { return null; }
    }

    public static bool GetBool(this JsonObject args, string key, bool fallback = false)
    {
        if (!args.TryGetPropertyValue(key, out var n) || n is null) return fallback;
        try
        {
            return n.GetValueKind() switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(n.GetValue<string>(), out var b) && b,
                _ => fallback,
            };
        }
        catch { return fallback; }
    }

    public static int? GetInt(this JsonObject args, string key)
    {
        if (!args.TryGetPropertyValue(key, out var n) || n is null) return null;
        try
        {
            return n.GetValueKind() switch
            {
                JsonValueKind.Number => n.GetValue<int>(),
                JsonValueKind.String => int.TryParse(n.GetValue<string>(), out var i) ? i : null,
                _ => null,
            };
        }
        catch { return null; }
    }

    public static double? GetDouble(this JsonObject args, string key)
    {
        if (!args.TryGetPropertyValue(key, out var n) || n is null) return null;
        try
        {
            return n.GetValueKind() switch
            {
                JsonValueKind.Number => n.GetValue<double>(),
                JsonValueKind.String => double.TryParse(n.GetValue<string>(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null,
                _ => null,
            };
        }
        catch { return null; }
    }

    /// <summary>Parses an ISO-8601 / common date string as UTC. Bare dates are treated as UTC midnight.</summary>
    public static DateTime? GetDateUtc(this JsonObject args, string key)
    {
        var raw = args.GetString(key);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
            ? dt
            : null;
    }
}
