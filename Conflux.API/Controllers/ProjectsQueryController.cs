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
public class ProjectsQueryController : ControllerBase
{
    private readonly ConfluxContext _context;

    public ProjectsQueryController(ConfluxContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all projects whose title contains the query, ignoring case
    /// </summary>
    /// <param name="query">The string on which to compare</param>
    /// <returns>Projects whose title contains the query</returns>
    [HttpGet]
    public async Task<IEnumerable<Project>> GetProjectByName(string query) =>
        (await _context.Projects
            .Include(p => p.People)
            .Include(p => p.Products)
            .Include(p => p.Parties)
            .ToListAsync())
        .Where(
            project => project.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase));

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
}
