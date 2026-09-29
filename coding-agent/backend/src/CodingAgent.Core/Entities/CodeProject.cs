namespace CodingAgent.Core.Entities;

/// <summary>
/// A local code repository the coding agent is allowed to work in. <see cref="RootPath"/> is an
/// absolute, normalized directory path; all file access is sandboxed to inside it.
/// </summary>
public class CodeProject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string RootPath { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastOpenedAt { get; set; } = DateTime.UtcNow;
}
