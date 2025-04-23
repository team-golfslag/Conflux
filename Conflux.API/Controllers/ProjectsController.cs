// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Models;
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
    /// Gets all projects whose title or description contains the query (case-insensitive),
    /// and optionally filters by start and/or end date.
    /// </summary>
    /// <param name="query">Optional: The string to search in the title or description</param>
    /// <param name="startDate">Optional: Only return projects starting on or after this date</param>
    /// <param name="endDate">Optional: Only return projects ending on or before this date</param>
    /// <returns>Filtered list of projects</returns>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(List<Project>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Project>>> GetProjectByQuery(
        [FromQuery] string? query,
        [FromQuery(Name = "start_date")] DateTime? startDate,
        [FromQuery(Name = "end_date")] DateTime? endDate) =>
        await _projectsService.GetProjectsByQueryAsync(query, startDate, endDate);

    /// <summary>
    /// Gets all projects
    /// </summary>
    /// <returns>All projects</returns>
    [HttpGet]
    [Authorize]
    [Route("all")]
    [ProducesResponseType(typeof(List<Project>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Project>>> GetAllProjects()
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
    [ProducesResponseType(typeof(Project), StatusCodes.Status200OK)]
    public async Task<ActionResult<Project>> GetProjectById([FromRoute] Guid id) =>
        await _projectsService.GetProjectByIdAsync(id);

    /// <summary>
    /// Puts a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update</param>
    /// <param name="projectDto">The new project details</param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{id:guid}")]
    [ProducesResponseType(typeof(Project), StatusCodes.Status200OK)]
    public async Task<ActionResult<Project>> PutProject([FromRoute] Guid id, ProjectPutDTO projectDto) =>
        await _projectsService.PutProjectAsync(id, projectDto);

    /// <summary>
    /// Patches a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update</param>
    /// <param name="projectDto">The new project details</param>
    /// <returns>The request response</returns>
    [HttpPatch]
    [Route("{id:guid}")]
    [ProducesResponseType(typeof(Project), StatusCodes.Status200OK)]
    public async Task<ActionResult<Project>> PatchProject([FromRoute] Guid id, ProjectPatchDTO projectDto) =>
        await _projectsService.PatchProjectAsync(id, projectDto);

    /// <summary>
    /// Updates a project by adding the contributor with the provided personId.
    /// </summary>
    /// <param name="projectId">The GUID of the project to update</param>
    /// <param name="contributorId">The GUID of the contributor to add to the project</param>
    /// <returns>The request response</returns>
    [HttpPost]
    [Route("{projectId:guid}/contributors")]
    [ProducesResponseType(typeof(Project), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Project>> AddContributorToProjectAsync([FromRoute] Guid projectId,
        [FromBody] Guid contributorId) =>
        await _projectsService.AddContributorToProjectAsync(projectId, contributorId);

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
