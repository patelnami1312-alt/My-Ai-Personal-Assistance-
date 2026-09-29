namespace CodingAgent.Infrastructure.Workspace;

/// <summary>Thrown when a requested path resolves outside its project root.</summary>
public class PathEscapeException(string message) : Exception(message);

/// <summary>
/// Resolves a caller-supplied relative path against a fixed project root and guarantees the
/// result stays inside that root. This is the security boundary for all file access - every
/// read must go through <see cref="Resolve"/>.
/// </summary>
public static class PathGuard
{
    /// <summary>Normalizes a user-entered root path to an absolute, trailing-separator-free form.</summary>
    public static string NormalizeRoot(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("root path is required");
        var full = Path.GetFullPath(rootPath.Trim());
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Combines <paramref name="root"/> with <paramref name="relative"/> (which may be null/empty
    /// for the root itself) and returns the absolute path, or throws if it would escape the root.
    /// </summary>
    public static string Resolve(string root, string? relative)
    {
        var normalizedRoot = NormalizeRoot(root);
        relative = (relative ?? "").Replace('\\', '/').Trim('/');

        if (relative.Split('/').Any(seg => seg is ".."))
            throw new PathEscapeException("path traversal ('..') is not allowed");

        var combined = string.IsNullOrEmpty(relative)
            ? normalizedRoot
            : Path.GetFullPath(Path.Combine(normalizedRoot, relative));

        var rootWithSep = normalizedRoot + Path.DirectorySeparatorChar;
        var isInside = combined.Equals(normalizedRoot, PathComparison)
                       || combined.StartsWith(rootWithSep, PathComparison);
        if (!isInside)
            throw new PathEscapeException($"'{relative}' resolves outside the project root");

        return combined;
    }

    /// <summary>Converts an absolute path inside <paramref name="root"/> back to a '/'-relative path.</summary>
    public static string ToRelative(string root, string absolute)
    {
        var rel = Path.GetRelativePath(NormalizeRoot(root), absolute);
        return rel == "." ? "" : rel.Replace('\\', '/');
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
