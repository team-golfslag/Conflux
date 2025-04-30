// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Services;
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
public class ContributorsController : ControllerBase
{
    private readonly ContributorsService _contributorsService;

    public ContributorsController(ContributorsService contributorsService)
    {
        _contributorsService = contributorsService;
    }

    /// <summary>
    /// Gets all contributors whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="query">Optional: The string to search in the title or description</param>
    /// <returns>Filtered list of contributors</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<Contributor>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Contributor>>> GetContributorsByQuery(Guid projectId,
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
    [ProducesResponseType(typeof(Contributor), StatusCodes.Status200OK)]
    public async Task<ActionResult<Contributor>> GetContributorByIdAsync([FromRoute] Guid projectId,
        [FromRoute] Guid personId) =>
        await _contributorsService.GetContributorByIdAsync(projectId, personId);

    /// <summary>
    /// Creates a new contributor via POST
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Contributor), StatusCodes.Status201Created)]
    public async Task<ActionResult<Contributor>> CreateContributor([FromRoute] Guid projectId,
        [FromBody] ContributorDTO contributorDTO) =>
        await _contributorsService.CreateContributorAsync(projectId, contributorDTO);

    /// <summary>
    /// Updates a contributor via PUT
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{personId:guid}")]
    [ProducesResponseType(typeof(Contributor), StatusCodes.Status200OK)]
    public async Task<ActionResult<Contributor>> UpdateContributor([FromRoute] Guid projectId,
        [FromRoute] Guid personId,
        [FromBody] ContributorDTO contributorDTO) =>
        await _contributorsService.UpdateContributorAsync(projectId, personId, contributorDTO);

    [HttpPatch]
    [Route("{personId:guid}")]
    [ProducesResponseType(typeof(Contributor), StatusCodes.Status200OK)]
    public async Task<ActionResult<Contributor>>
        PatchContributor([FromRoute] Guid projectId, [FromRoute] Guid personId,
            [FromBody] ContributorPatchDTO contributorDTO) =>
        await _contributorsService.PatchContributorAsync(projectId, personId, contributorDTO);
}
