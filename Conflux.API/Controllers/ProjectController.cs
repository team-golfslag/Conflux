using Conflux.API.DTOs;
using Conflux.API.Services;
using Conflux.Data;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for managing projects
/// </summary>
[Route("project")]
[ApiController]
public class ProjectController : ControllerBase
{
    private readonly ConfluxContext _context;
    private readonly ProjectService _projectService;

    public ProjectController(ConfluxContext context)
    {
        _context = context;
        _projectService = new(_context);
    }
    
    /// <summary>
    /// Gets a project by its GUID.
    /// </summary>
    [HttpGet]
    [Route("{id:guid}")]
    public ActionResult<Project> GetProjectById([FromRoute] Guid id)
    {
        Project? project = _context.Projects
            .Include(p => p.People)
            .Include(p => p.Products)
            .Include(p => p.Parties)
            .FirstOrDefault(p => p.Id == id);
        return project == null ? NotFound() : Ok(project);
    } 

    /// <summary>
    /// Creates a new project
    /// </summary>
    /// <param name="projectDto">The DTO which to convert to a <see cref="Project"/></param>
    /// <returns>The request response</returns>
    [HttpPost]
    [Route("create")]
    public ActionResult CreateProject([FromBody] ProjectDto projectDto)
    {
        Project project = projectDto.ToProject();
        _context.Projects.Add(project);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
    }

    /// <summary>
    /// Updates a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update</param>
    /// <param name="projectDto">The new project details</param>
    /// <returns>The request response</returns>
    [HttpPost]
    [Route("update/{id:guid}")]
    public async Task<ActionResult> UpdateProject([FromRoute] Guid id, ProjectUpdateDto projectDto)
    {
        Project? updateProject = await _projectService.UpdateProjectAsync(id, projectDto);
        return updateProject == null ? NotFound() : Ok(updateProject);
    }
}
