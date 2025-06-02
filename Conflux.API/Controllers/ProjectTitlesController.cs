// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

[Route("projects/{projectId:guid}/titles")]
[ApiController]
[Authorize]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
[RouteParamName("projectId")]
public class ProjectTitlesController : ControllerBase
{
    private readonly IProjectTitlesService _titlesService;

    public ProjectTitlesController(IProjectTitlesService service)
    {
        _titlesService = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [RequireProjectRole(UserRoleType.User)]
    public async Task<ActionResult<List<ProjectTitleResponseDTO>>> GetTitlesByProject([FromRoute] Guid projectId) =>
        await _titlesService.GetTitlesByProjectIdAsync(projectId);

    [HttpGet]
    [Route("current/{titleType:alpha}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [RequireProjectRole(UserRoleType.User)]
    public async Task<ActionResult<ProjectTitleResponseDTO?>>
        GetCurrentTitleByType([FromRoute] Guid projectId, [FromRoute] TitleType titleType) =>
        await _titlesService.GetCurrentTitleByTitleType(projectId, titleType);

    [HttpGet]
    [Route("{titleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [RequireProjectRole(UserRoleType.User)]
    public async Task<ActionResult<ProjectTitleResponseDTO>> GetTitleById([FromRoute] Guid projectId,
        [FromRoute] Guid titleId) =>
        await _titlesService.GetTitleByIdAsync(projectId, titleId);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [RequireProjectRole(UserRoleType.Admin)]
    public async Task<ActionResult<List<ProjectTitleResponseDTO>>> UpdateTitle([FromRoute] Guid projectId,
        [FromBody] ProjectTitleRequestDTO titleDTO) =>
        await _titlesService.UpdateTitleAsync(projectId, titleDTO);

    [HttpPost]
    [Route("{titleId:guid}/end")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [RequireProjectRole(UserRoleType.Admin)]
    public async Task<ActionResult<ProjectTitleResponseDTO>> EndTitle([FromRoute] Guid projectId,
        [FromRoute] Guid titleId) =>
        await _titlesService.EndTitleAsync(projectId, titleId);
    

    [HttpDelete]
    [Route("{titleId:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [RequireProjectRole(UserRoleType.Admin)]
    public async Task<ActionResult> DeleteTitle([FromRoute] Guid projectId,
        [FromRoute] Guid titleId)
    {
        await _titlesService.DeleteTitleAsync(projectId, titleId);

        return Ok();
    }
}
