using Conflux.Data;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Core.Controllers;

/// <summary>
/// Represents the controller for querying projects
/// </summary>
[Route("projects/query/[controller]")]
[ApiController]
public class ProjectQueryController : ControllerBase
{
    private readonly ConfluxContext _context;

    public ProjectQueryController(ConfluxContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IEnumerable<Project>> GetProjectByName(string query) =>
        (await _context.Projects.Include(p => p.People).Include(p => p.Products).ToListAsync()).Where(
            project => project.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase));
}
