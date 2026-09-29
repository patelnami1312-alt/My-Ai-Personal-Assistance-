using System.Text;
using CodingAgent.Core.Dtos;

namespace CodingAgent.Infrastructure.Workspace;

public interface IWorkspaceService
{
    /// <summary>Lists the immediate children of <paramref name="relativeDir"/> (null/"" = root).</summary>
    IReadOnlyList<FileTreeNode> ListDirectory(string root, string? relativeDir);

    /// <summary>Reads a text file, refusing binaries and capping size.</summary>
    FileContentDto ReadFile(string root, string relativePath);
}

public class WorkspaceService : IWorkspaceService
{
    private const long MaxReadableBytes = 1_000_000; // 1 MB
    private const int MaxDirEntries = 2000;

    private static readonly HashSet<string> IgnoredDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", "node_modules", "bin", "obj", ".vs", ".idea", "dist", "build",
        ".next", ".nuxt", "__pycache__", ".venv", "venv", ".gradle", "target",
    };

    public IReadOnlyList<FileTreeNode> ListDirectory(string root, string? relativeDir)
    {
        var absDir = PathGuard.Resolve(root, relativeDir);
        if (!Directory.Exists(absDir))
            throw new DirectoryNotFoundException($"'{relativeDir}' is not a directory in this project");

        var entries = new List<FileTreeNode>();
        foreach (var dir in Directory.EnumerateDirectories(absDir).Take(MaxDirEntries))
        {
            var name = Path.GetFileName(dir);
            if (name.StartsWith('.') && name is not ".github") continue;
            if (IgnoredDirs.Contains(name)) continue;
            entries.Add(new FileTreeNode(name, PathGuard.ToRelative(root, dir), true, 0, DirHasChildren(dir)));
        }
        foreach (var file in Directory.EnumerateFiles(absDir).Take(MaxDirEntries))
        {
            var info = new FileInfo(file);
            entries.Add(new FileTreeNode(info.Name, PathGuard.ToRelative(root, file), false, info.Length, false));
        }

        return entries
            .OrderByDescending(e => e.IsDirectory)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public FileContentDto ReadFile(string root, string relativePath)
    {
        var abs = PathGuard.Resolve(root, relativePath);
        if (!File.Exists(abs))
            throw new FileNotFoundException($"'{relativePath}' not found in this project");

        var info = new FileInfo(abs);
        var truncated = info.Length > MaxReadableBytes;

        using var stream = File.OpenRead(abs);
        var buffer = new byte[Math.Min(info.Length, MaxReadableBytes)];
        var read = stream.Read(buffer, 0, buffer.Length);

        if (LooksBinary(buffer.AsSpan(0, read)))
            throw new InvalidOperationException($"'{relativePath}' looks like a binary file and can't be shown");

        var text = new UTF8Encoding(false, false).GetString(buffer, 0, read);
        var lineCount = text.Count(c => c == '\n') + 1;

        return new FileContentDto(
            PathGuard.ToRelative(root, abs),
            text,
            info.Length,
            lineCount,
            LanguageFor(Path.GetExtension(abs)),
            truncated);
    }

    private static bool DirHasChildren(string dir)
    {
        try
        {
            foreach (var _ in Directory.EnumerateDirectories(dir)) return true;
            foreach (var _ in Directory.EnumerateFiles(dir)) return true;
        }
        catch (UnauthorizedAccessException) { }
        return false;
    }

    private static bool LooksBinary(ReadOnlySpan<byte> bytes)
    {
        var limit = Math.Min(bytes.Length, 8000);
        for (var i = 0; i < limit; i++)
            if (bytes[i] == 0) return true;
        return false;
    }

    private static string LanguageFor(string ext) => ext.ToLowerInvariant() switch
    {
        ".cs" => "csharp",
        ".ts" or ".tsx" => "typescript",
        ".js" or ".jsx" or ".mjs" or ".cjs" => "javascript",
        ".json" => "json",
        ".py" => "python",
        ".java" => "java",
        ".go" => "go",
        ".rs" => "rust",
        ".rb" => "ruby",
        ".php" => "php",
        ".html" or ".htm" => "html",
        ".css" => "css",
        ".scss" or ".sass" => "scss",
        ".md" or ".markdown" => "markdown",
        ".yml" or ".yaml" => "yaml",
        ".xml" or ".csproj" or ".props" or ".targets" => "xml",
        ".sql" => "sql",
        ".sh" or ".bash" => "bash",
        ".ps1" => "powershell",
        _ => "plaintext",
    };
}
