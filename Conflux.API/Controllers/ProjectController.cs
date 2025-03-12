using Conflux.Core.DTOs;
using Conflux.Data;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.Core.Controllers;

/// <summary>
/// Represents the controller for managing projects
/// </summary>
[Route("project")]
[ApiController]
public class ProjectController : ControllerBase
{
    private readonly ConfluxContext _context;

    public ProjectController(ConfluxContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new project
    /// </summary>
    /// <param name="projectDto">The DTO which to convert to a <see cref="Project"/></param>
    /// <returns>The request response</returns>
    [HttpPost]
    [Route("create")]
    public ActionResult CreateProject(ProjectDTO projectDto)
    {
        Project project = projectDto.ToProject();
        _context.Projects.Add(project);
        _context.SaveChanges();
        return Created();
    }
}
