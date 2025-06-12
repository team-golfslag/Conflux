// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text;
using Conflux.API.Attributes;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Queries;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for querying projects
/// </summary>
[Route("projects/")]
[Authorize]
[ApiController]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class ProjectsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ISRAMProjectSyncService _iSRAMProjectSyncService;
    private readonly IProjectsService _projectsService;
    private readonly ITimelineService _timelineService;
    private readonly IUserSessionService _userSessionService;
    
    public ProjectsController(IProjectsService projectsService, ISRAMProjectSyncService iSRAMProjectSyncService,
        IUserSessionService userSessionService, ITimelineService timelineService, IConfiguration configuration)
    {
        _iSRAMProjectSyncService = iSRAMProjectSyncService;
        _projectsService = projectsService;
        _userSessionService = userSessionService;
        _timelineService = timelineService;
        _configuration = configuration;
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
    [HttpGet]
    [ProducesResponseType(typeof(List<ProjectResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProjectResponseDTO>>> GetProjectByQuery(
        ProjectQueryDTO projectQueryDto) =>
        await _projectsService.GetProjectsByQueryAsync(projectQueryDto);
    
    /// <summary>
    /// Gets the timeline items for a specific project by its GUID.
    /// </summary>
    /// <param name="id">The GUID of the project.</param>
    /// <returns>
    /// A list of <see cref="TimelineItemResponseDTO" /> representing the timeline items for the project.
    /// </returns>
    [HttpGet]
    [Route("timeline")]
    [ProducesResponseType(typeof(List<TimelineItemResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TimelineItemResponseDTO>>> GetProjectTimeline([FromQuery] Guid id)
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            return Unauthorized();
        
        return await _timelineService.GetTimelineItemsAsync(id);
    }
    
    
    /// <summary>
    /// Exports projects as a CSV file based on the provided query parameters and returns it as a downloadable file.
    /// </summary>
    /// <param name="projectQueryDto">
    /// The <see cref="ProjectQueryDTO" /> containing the query term and optional filters for
    /// exporting projects to CSV.
    /// </param>
    /// <returns>CSV file containing the exported projects.</returns>
    [Authorize]
    [HttpGet]
    [Route("export")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<ActionResult> ExportToCsv([FromQuery] ProjectCsvRequestDTO projectQueryDto)
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            return Unauthorized();
        
        string csv = await _projectsService.ExportProjectsToCsvAsync(projectQueryDto);
        string fileName = $"projects_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }
    
    /// <summary>
    /// Gets all projects
    /// </summary>
    /// <returns>All projects</returns>
    [HttpGet]
    [Route("all")]
    [ProducesResponseType(typeof(List<ProjectResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProjectResponseDTO>>> GetAllProjects()
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
    [Route("{id:guid}")]
    [RouteParamName("id")]
    [RequireProjectRole(UserRoleType.User)]
    [ProducesResponseType(typeof(ProjectResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectResponseDTO>> GetProjectById([FromRoute] Guid id) =>
        await _projectsService.GetProjectDTOByIdAsync(id);
    
    /// <summary>
    /// Favorites a project by its GUID.
    /// </summary>
    /// <param name="id">The GUID of the project to favorite</param>
    /// <param name="favorite">Whether to favorite or unfavorite the project</param>
    [HttpPost]
    [Route("{id:guid}/favorite")]
    [RouteParamName("id")]
    [RequireProjectRole(UserRoleType.User)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> FavoriteProject([FromRoute] Guid id, [FromQuery] bool favorite)
    {
        await _projectsService.FavoriteProjectAsync(id, favorite);
        return Ok();
    }
    
    /// <summary>
    /// Puts a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update</param>
    /// <param name="projectDto">The new project details</param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{id:guid}")]
    [RouteParamName("id")]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(ProjectResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectResponseDTO>> PutProject([FromRoute] Guid id, ProjectRequestDTO projectDto) =>
        await _projectsService.PutProjectAsync(id, projectDto);
    
    [HttpPost]
    [Route("{id:guid}/sync")]
    [RouteParamName("id")]
    [RequireProjectRole(UserRoleType.User)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> SyncProject([FromRoute] Guid id)
    {
        await _iSRAMProjectSyncService.SyncProjectAsync(id);
        return Ok();
    }
    
    /// <summary>
    /// Gets the list of lectorates from the configuration.
    /// </summary>
    [HttpGet]
    [Route("lectorates")]
    public List<string> GetLectorates() =>
        _configuration.GetSection("Lectorates").Get<List<string>>() ?? new List<string>();
}
