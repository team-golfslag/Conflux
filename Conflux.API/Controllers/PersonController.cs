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
[Route("person")]
[ApiController]
public class PersonController : ControllerBase
{
    private readonly ConfluxContext _context;

    public PersonController(ConfluxContext context)
    {
        _context = context;
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
}
