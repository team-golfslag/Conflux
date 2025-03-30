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
    /// Gets all projects whose title or description contains the query (case-insensitive),
    /// and optionally filters by start and/or end date.
    /// </summary>
    /// <param name="query">Optional: The string to search in the title or description</param>
    /// <param name="startDate">Optional: Only return projects starting on or after this date</param>
    /// <param name="endDate">Optional: Only return projects ending on or before this date</param>
    /// <returns>Filtered list of projects</returns>
    [HttpGet]
    public async Task<IEnumerable<Project>> GetProjectByQuery(
        [FromQuery] string? query,
        [FromQuery(Name = "start_date")] DateTime? startDate,
        [FromQuery(Name = "end_date")] DateTime? endDate)
    {
        IQueryable<Project> projects = _context.Projects
            .Include(p => p.People)
            .Include(p => p.Products)
            .Include(p => p.Parties);

        if (!string.IsNullOrWhiteSpace(query))
        {
            string loweredQuery = query.ToLowerInvariant();
#pragma warning disable CA1862
            projects = projects.Where(project =>
                project.Title.ToLower().Contains(loweredQuery) ||
                (project.Description ?? "").ToLower().Contains(loweredQuery));
#pragma warning restore CA1862
        }

        if (startDate.HasValue)
        {
            startDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            projects = projects.Where(project => project.StartDate != null && project.StartDate >= startDate);
        }

        if (endDate.HasValue)
        {
            endDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
            projects = projects.Where(project => project.EndDate != null && project.EndDate <= endDate);
        }

        if (startDate.HasValue && endDate.HasValue)
            projects = projects.Where(project => project.StartDate <= endDate && project.EndDate >= startDate);

        return await projects.ToListAsync();
    }

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
