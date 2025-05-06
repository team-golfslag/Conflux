// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for querying projects
/// </summary>
[Route("projects/")]
[ApiController]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class ProjectsController : ControllerBase
{
    private readonly ISRAMProjectSyncService _iSRAMProjectSyncService;
    private readonly ProjectsService _projectsService;
    private readonly IUserSessionService _userSessionService;

    public ProjectsController(ProjectsService projectsService, ISRAMProjectSyncService iSRAMProjectSyncService,
        IUserSessionService userSessionService)
    {
        _iSRAMProjectSyncService = iSRAMProjectSyncService;
        _projectsService = projectsService;
        _userSessionService = userSessionService;
    }

    /// <summary>
    /// Gets all projects whose title or description contains the query (case-insensitive)
    /// and optionally filters by start and/or end date.
    /// </summary>
    /// <param name="projectQueryDto">
    /// The <see cref="ProjectQueryDTO" /> that contains the query term, filters and 'order by' method for
    /// the query
    /// </param>
    /// <returns>Filtered list of projects</returns>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(List<ProjectDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProjectDTO>>> GetProjectByQuery(
        ProjectQueryDTO projectQueryDto) =>
        await _projectsService.GetProjectsByQueryAsync(projectQueryDto);

    /// <summary>
    /// Gets all projects
    /// </summary>
    /// <returns>All projects</returns>
    [HttpGet]
    [Authorize]
    [Route("all")]
    [ProducesResponseType(typeof(List<Project>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProjectDTO>>> GetAllProjects()
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            return Unauthorized();
        return await _projectsService.GetAllProjectsAsync();
    }

    /// <summary>
    /// Gets a project by its GUID.
    /// </summary>
    [HttpGet]
    [Authorize]
    [Route("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDTO>> GetProjectById([FromRoute] Guid id) =>
        await _projectsService.GetProjectByIdAsync(id);

    /// <summary>
    /// Puts a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update</param>
    /// <param name="projectDto">The new project details</param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDTO>> PutProject([FromRoute] Guid id, ProjectDTO projectDto) =>
        await _projectsService.PutProjectAsync(id, projectDto);

    /// <summary>
    /// Patches a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update</param>
    /// <param name="projectDto">The new project details</param>
    /// <returns>The request response</returns>
    [HttpPatch]
    [Route("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDTO>> PatchProject([FromRoute] Guid id, ProjectPatchDTO projectDto) =>
        await _projectsService.PatchProjectAsync(id, projectDto);

    [HttpPost]
    [Route("{id:guid}/sync")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> SyncProject([FromRoute] Guid id)
    {
        await _iSRAMProjectSyncService.SyncProjectAsync(id);
        return Ok();
    }
}
