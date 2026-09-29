namespace CodingAgent.Core.Dtos;

public record RegisterProjectRequest(string Name, string RootPath, string? Description);

public record ProjectDto(Guid Id, string Name, string RootPath, string? Description, DateTime CreatedAt, DateTime LastOpenedAt);

/// <summary>One entry in a directory listing. Path is relative to the project root, using '/'.</summary>
public record FileTreeNode(string Name, string Path, bool IsDirectory, long Size, bool HasChildren);

public record FileContentDto(string Path, string Text, long Size, int LineCount, string Language, bool Truncated);
