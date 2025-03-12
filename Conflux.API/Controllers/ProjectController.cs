using Conflux.Core.DTOs;
using Conflux.Data;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.Core.Controllers;

[Route("project")]
[ApiController]
public class ProjectController : ControllerBase
{
    private readonly ConfluxContext _context;

    public ProjectController(ConfluxContext context)
    {
        _context = context;
    }

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
