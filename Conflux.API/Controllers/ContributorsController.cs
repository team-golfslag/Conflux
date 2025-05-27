// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for managing contributors
/// </summary>
[ApiController]
[Authorize]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
[Route("projects/{projectId:guid}/contributors")]
[RouteParamName("projectId")]
public class ContributorsController : ControllerBase
{
    private readonly IContributorsService _contributorsService;

    public ContributorsController(IContributorsService contributorsService)
    {
        _contributorsService = contributorsService;
    }

    /// <summary>
    /// Gets all contributors whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="query">Optional: The string to search in the title or description</param>
    /// <returns>Filtered list of contributors</returns>
    [HttpGet]
    [RequireProjectRole(UserRoleType.User)]
    [ProducesResponseType(typeof(List<ContributorResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ContributorResponseDTO>>> GetContributorsByQuery(Guid projectId,
        [FromQuery] string? query) =>
        await _contributorsService.GetContributorsByQueryAsync(projectId, query);

    /// <summary>
    /// Gets the contributor by his GUID
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    /// <returns>The request response</returns>
    [HttpGet]
    [Route("{personId:guid}")]
    [RequireProjectRole(UserRoleType.User)]
    [ProducesResponseType(typeof(ContributorResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContributorResponseDTO>> GetContributorByIdAsync([FromRoute] Guid projectId,
        [FromRoute] Guid personId) =>
        await _contributorsService.GetContributorByIdAsync(projectId, personId);

    /// <summary>
    /// Deletes a contributor
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    [HttpDelete]
    [Route("{personId:guid}")]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteContributor([FromRoute] Guid projectId,
        [FromRoute] Guid personId)
    {
        try
        {
            await _contributorsService.DeleteContributorAsync(projectId, personId);
        }
        catch (ContributorNotFoundException)
        {
            return NotFound("Contributor not found");
        }

        return Ok();
    }

    /// <summary>
    /// Creates a new contributor via POST
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPost]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(ContributorResponseDTO), StatusCodes.Status201Created)]
    public async Task<ActionResult<ContributorResponseDTO>> CreateContributor([FromRoute] Guid projectId,
        [FromRoute] Guid personId,
        [FromBody] ContributorRequestDTO contributorDTO) =>
        await _contributorsService.CreateContributorAsync(projectId, personId, contributorDTO);

    /// <summary>
    /// Updates a contributor via PUT
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{personId:guid}")]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(ContributorResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContributorResponseDTO>> UpdateContributor([FromRoute] Guid projectId,
        [FromRoute] Guid personId,
        [FromBody] ContributorRequestDTO contributorDTO) =>
        await _contributorsService.UpdateContributorAsync(projectId, personId, contributorDTO);
}
