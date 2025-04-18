// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Services;
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

    public ProjectsController(ConfluxContext context)
    {
        _projectsService = new(context);
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
    [Route("all")]
    public async Task<ActionResult<List<Project>>> GetAllProjects() =>
        await _projectsService.GetAllProjectsAsync();

    /// <summary>
    /// Gets a project by its GUID.
    /// </summary>
    [HttpGet]
    [Route("{id:guid}")]
    public async Task<ActionResult<Project>> GetProjectById([FromRoute] Guid id) =>
        await _projectsService.GetProjectByIdAsync(id);

    /// <summary>
    /// Creates a new project
    /// </summary>
    /// <param name="projectPostDto">The DTO which to convert to a <see cref="Project" /></param>
    /// <returns>The request response</returns>
    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject([FromBody] ProjectPostDTO projectPostDto) =>
        await _projectsService.CreateProjectAsync(projectPostDto);

    /// <summary>
    /// Puts a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update</param>
    /// <param name="projectDto">The new project details</param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{id:guid}")]
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
}
