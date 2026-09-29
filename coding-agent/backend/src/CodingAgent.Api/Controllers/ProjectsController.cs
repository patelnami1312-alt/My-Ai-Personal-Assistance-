using CodingAgent.Core.Dtos;
using CodingAgent.Core.Entities;
using CodingAgent.Infrastructure.Data;
using CodingAgent.Infrastructure.Workspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Api.Controllers;

/// <summary>Phase 1 - coding workspace: register projects and browse/read their files (sandboxed).</summary>
[ApiController]
[Route("api/projects")]
public class ProjectsController(CodingAgentDbContext db, IWorkspaceService workspace, ILogger<ProjectsController> log)
    : ControllerBase
{
    private static ProjectDto ToDto(CodeProject p) =>
        new(p.Id, p.Name, p.RootPath, p.Description, p.CreatedAt, p.LastOpenedAt);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var projects = await db.Projects.OrderByDescending(p => p.LastOpenedAt).ToListAsync(ct);
        return Ok(projects.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterProjectRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.RootPath))
            return BadRequest(new { error = "name and rootPath are required" });

        string root;
        try { root = PathGuard.NormalizeRoot(req.RootPath); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }

        if (!Directory.Exists(root))
            return BadRequest(new { error = $"directory does not exist: {root}" });

        if (await db.Projects.AnyAsync(p => p.RootPath == root, ct))
            return Conflict(new { error = "a project is already registered at that path" });

        var project = new CodeProject
        {
            Name = req.Name.Trim(),
            RootPath = root,
            Description = req.Description,
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = project.Id }, ToDto(project));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var project = await Find(id, ct);
        if (project is null) return NotFound();
        project.LastOpenedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(project));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Unregister(Guid id, CancellationToken ct)
    {
        var project = await Find(id, ct);
        if (project is null) return NotFound();
        db.Projects.Remove(project); // only removes the registration; never touches files on disk
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Lists the immediate children of a directory inside the project (lazy tree).</summary>
    [HttpGet("{id:guid}/tree")]
    public async Task<IActionResult> Tree(Guid id, [FromQuery] string? path, CancellationToken ct)
    {
        var project = await Find(id, ct);
        if (project is null) return NotFound();
        try
        {
            return Ok(workspace.ListDirectory(project.RootPath, path));
        }
        catch (PathEscapeException ex) { return BadRequest(new { error = ex.Message }); }
        catch (DirectoryNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex)
        {
            log.LogWarning(ex, "tree listing failed for project {Id}", id);
            return StatusCode(500, new { error = "could not read that directory" });
        }
    }

    /// <summary>Returns the text content of a file inside the project.</summary>
    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> File(Guid id, [FromQuery] string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path)) return BadRequest(new { error = "path is required" });
        var project = await Find(id, ct);
        if (project is null) return NotFound();
        try
        {
            return Ok(workspace.ReadFile(project.RootPath, path));
        }
        catch (PathEscapeException ex) { return BadRequest(new { error = ex.Message }); }
        catch (FileNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        catch (Exception ex)
        {
            log.LogWarning(ex, "file read failed for project {Id}", id);
            return StatusCode(500, new { error = "could not read that file" });
        }
    }

    private Task<CodeProject?> Find(Guid id, CancellationToken ct) =>
        db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
}
