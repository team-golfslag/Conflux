using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for querying projects
/// </summary>
[Route("projects/")]
[ApiController]
public class ProjectsController : ControllerBase
{
    private readonly ConfluxContext _context;
    private readonly ProjectsService _projectsService;

    public ProjectsController(ConfluxContext context)
    {
        _context = context;
        _projectsService = new(_context);
    }

    /// <summary>
    /// Gets all projects whose title contains the query, ignoring case
    /// </summary>
    /// <param name="query">The string on which to compare</param>
    /// <returns>Projects whose title contains the query</returns>
    [HttpGet]
    public async Task<IEnumerable<Project>> GetProjectByName(string query) =>
        await _context.Projects
            .Include(p => p.People)
            .Include(p => p.Products)
            .Include(p => p.Parties)
            .Where(
#pragma warning disable CA1862 // Contains with StringComparison.CurrentCultureIgnoreCase cannot be converted to SQL
                project => project.Title.Contains(query.ToLowerInvariant()))
#pragma warning restore CA1862
            .ToListAsync();

    /// <summary>
    /// Gets all projects
    /// </summary>
    /// <returns>All projects</returns>
    [HttpGet]
    [Route("all")]
    public async Task<IEnumerable<Project>> GetAllProjects()
    {
        return await _context.Projects
            .Include(p => p.Products)
            .Include(p => p.People)
            .Include(p => p.Parties)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a project by its GUID.
    /// </summary>
    [HttpGet]
    [Route("{id:guid}")]
    public async Task<ActionResult<Project>> GetProjectById([FromRoute] Guid id)
    {
        Project? project = await _context.Projects
            .Include(p => p.People)
            .Include(p => p.Products)
            .Include(p => p.Parties)
            .SingleOrDefaultAsync(p => p.Id == id);
        return project == null ? NotFound() : Ok(project);
    }

    /// <summary>
    /// Creates a new project
    /// </summary>
    /// <param name="projectPostDto">The DTO which to convert to a <see cref="Project" /></param>
    /// <returns>The request response</returns>
    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject([FromBody] ProjectPostDTO projectPostDto)
    {
        Project project = projectPostDto.ToProject();
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProjectById), new
        {
            id = project.Id,
        }, project);
    }

    /// <summary>
    /// Puts a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update</param>
    /// <param name="projectDto">The new project details</param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{id:guid}")]
    public async Task<ActionResult> PutProject([FromRoute] Guid id, ProjectPutDTO projectDto)
    {
        Project updateProject = await _projectsService.PutProjectAsync(id, projectDto);
        return Ok(updateProject);
    }

    /// <summary>
    /// Patches a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update</param>
    /// <param name="projectDto">The new project details</param>
    /// <returns>The request response</returns>
    [HttpPatch]
    [Route("{id:guid}")]
    public async Task<ActionResult> PatchProject([FromRoute] Guid id, ProjectPatchDTO projectDto)
    {
        Project updateProject = await _projectsService.PatchProjectAsync(id, projectDto);
        return Ok(updateProject);
    }
}
