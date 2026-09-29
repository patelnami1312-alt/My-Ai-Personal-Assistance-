using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Assistant.Infrastructure.Tools;

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

    /// <summary>Parses an ISO-8601 / common datetime string. Values without an offset are
    /// interpreted in <paramref name="tz"/> and returned as UTC.</summary>
    public static DateTime? GetDateTimeUtc(this JsonObject args, string key, string tz)
    {
        var raw = args.GetString(key);
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto)
            && (raw.Contains('Z') || raw.Contains('+') || raw.LastIndexOf('-') > 7))
        {
            return dto.UtcDateTime;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            try
            {
                var tzi = TimeZoneInfo.FindSystemTimeZoneById(tz);
                return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), tzi);
            }
            catch
            {
                return DateTime.SpecifyKind(local, DateTimeKind.Utc);
            }
        }

        return null;
    }
}
