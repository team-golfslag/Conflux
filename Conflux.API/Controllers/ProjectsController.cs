// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
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
public class ProjectsController : ControllerBase
{
    private readonly ProjectsService _projectsService;
    private readonly IProjectSyncService _projectSyncService;
    private readonly IUserSessionService _userSessionService;

    public ProjectsController(ProjectsService projectsService, IProjectSyncService projectSyncService,
        IUserSessionService userSessionService)
    {
        _projectSyncService = projectSyncService;
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
    public async Task<ActionResult<List<Project>>> GetProjectByQuery(
        [FromQuery] string? query,
        [FromQuery(Name = "start_date")] DateTime? startDate,
        [FromQuery(Name = "end_date")] DateTime? endDate) =>
        Ok(await _projectsService.GetProjectsByQueryAsync(query, startDate, endDate));

    /// <summary>
    /// Gets all projects
    /// </summary>
    /// <returns>All projects</returns>
    [HttpGet]
    [Authorize]
    [Route("all")]
    public async Task<ActionResult<List<Project>>> GetAllProjects()
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            return Unauthorized();
        var projects = await _projectsService.GetAllProjectsAsync();
        var projectDtos = new List<ProjectGetDTO>();
        foreach (Project project in projects)
        {
            Collaboration? collaborations =
                userSession.Collaborations.FirstOrDefault(c => c.CollaborationGroup.SRAMId == project.SRAMId);
            if (collaborations is null)
                continue;
            var roles = await _projectsService.GetRolesFromProject(project);
            if (roles is null)
                continue;
            projectDtos.Add(ProjectGetDTO.FromProject(project, roles));
        }
        
        return Ok(projectDtos);
    }

    /// <summary>
    /// Gets a project by its GUID.
    /// </summary>
    [HttpGet]
    [Authorize]
    [Route("{id:guid}")]
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
    public async Task<ActionResult<Project>> PutProject([FromRoute] Guid id, ProjectPutDTO projectDto) =>
        Ok(await _projectsService.PutProjectAsync(id, projectDto));

    /// <summary>
    /// Patches a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update</param>
    /// <param name="projectDto">The new project details</param>
    /// <returns>The request response</returns>
    [HttpPatch]
    [Route("{id:guid}")]
    public async Task<ActionResult<Project>> PatchProject([FromRoute] Guid id, ProjectPatchDTO projectDto) =>
        Ok(await _projectsService.PatchProjectAsync(id, projectDto));

    /// <summary>
    /// Updates a project by adding the person with the provided personId.
    /// </summary>
    /// <param name="projectId">The GUID of the project to update</param>
    /// <param name="personId">The GUID of the person to add to the project</param>
    /// <returns>The request response</returns>
    [HttpPost]
    [Route("{projectId:guid}/addPerson/{personId:guid}")]
    public async Task<ActionResult<Project>> AddPersonToProjectAsync([FromRoute] Guid projectId, Guid personId) =>
        await _projectsService.AddPersonToProjectAsync(projectId, personId);

    [HttpPost]
    [Route("{id:guid}/sync")]
    [Authorize]
    public async Task<ActionResult> SyncProject([FromRoute] Guid id)
    {
        await _projectSyncService.SyncProjectAsync(id);
        return Ok();
    }
}
