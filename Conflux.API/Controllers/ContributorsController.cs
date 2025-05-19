// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for managing contributors
/// </summary>
[ApiController]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
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
    /// <param name="query">Optional: The string to search in the title or description</param>
    /// <returns>Filtered list of contributors</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ContributorDTO>), StatusCodes.Status200OK)]
    [RequireProjectRole(UserRoleType.User)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ContributorDTO>>> GetContributorsByQuery(Guid projectId,
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
    [ProducesResponseType(typeof(ContributorDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContributorDTO>> GetContributorByIdAsync([FromRoute] Guid projectId,
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPost]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(ContributorDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContributorDTO>> CreateContributor([FromRoute] Guid projectId,
        [FromBody] ContributorDTO contributorDTO)
    {
        if (contributorDTO.ProjectId != projectId)
            return BadRequest("Project ID in the URL does not match the one in the body.");
        try
        {
            return await _contributorsService.CreateContributorAsync(contributorDTO);
        } catch (ContributorAlreadyExistsException)
        {
            return Conflict("Contributor already exists");
        }
    }

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
    [ProducesResponseType(typeof(ContributorDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContributorDTO>> UpdateContributor([FromRoute] Guid projectId,
        [FromRoute] Guid personId,
        [FromBody] ContributorDTO contributorDTO) =>
        await _contributorsService.UpdateContributorAsync(projectId, personId, contributorDTO);

    [HttpPatch]
    [Route("{personId:guid}")]
    [RequireProjectRole(UserRoleType.Admin)]
    [ProducesResponseType(typeof(ContributorDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContributorDTO>>
        PatchContributor([FromRoute] Guid projectId, [FromRoute] Guid personId,
            [FromBody] ContributorPatchDTO contributorDTO) =>
        await _contributorsService.PatchContributorAsync(projectId, personId, contributorDTO);
}
