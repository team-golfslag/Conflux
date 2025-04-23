// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for managing contributors
/// </summary>
[Route("contributors")]
[ApiController]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class ContributorsController : ControllerBase
{
    private readonly ContributorsService _contributorsService;

    public ContributorsController(ConfluxContext context)
    {
        _contributorsService = new(context);
    }

    /// <summary>
    /// Gets all contributors whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="query">Optional: The string to search in the title or description</param>
    /// <returns>Filtered list of contributors</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<Contributor>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Contributor>>> GetProjectByQuery(
        [FromQuery] string? query) =>
        await _contributorsService.GetContributorsByQueryAsync(query);

    /// <summary>
    /// Gets the contributor by his GUID
    /// </summary>
    /// <param name="id">The GUID of the contributor</param>
    /// <returns>The request response</returns>
    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType(typeof(Contributor), StatusCodes.Status200OK)]
    public async Task<ActionResult<Contributor>> GetContributorByIdAsync([FromRoute] Guid id) =>
        await _contributorsService.GetContributorByIdAsync(id);

    /// <summary>
    /// Creates a new contributor via POST
    /// </summary>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Contributor), StatusCodes.Status201Created)]
    public async Task<ActionResult<Contributor>> CreateContributor([FromBody] ContributorPostDTO contributorDTO) =>
        await _contributorsService.CreateContributorAsync(contributorDTO);

    /// <summary>
    /// Updates a contributor via PUT
    /// </summary>
    /// <param name="id">The GUID of the contributor</param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{id:guid}")]
    [ProducesResponseType(typeof(Contributor), StatusCodes.Status200OK)]
    public async Task<ActionResult<Contributor>> UpdateContributor([FromRoute] Guid id,
        [FromBody] ContributorPutDTO contributorDTO) =>
        await _contributorsService.UpdateContributorAsync(id, contributorDTO);

    [HttpPatch]
    [Route("{id:guid}")]
    [ProducesResponseType(typeof(Contributor), StatusCodes.Status200OK)]
    public async Task<ActionResult<Contributor>>
        PatchContributor([FromRoute] Guid id, [FromBody] ContributorPatchDTO contributorDTO) =>
        await _contributorsService.PatchContributorAsync(id, contributorDTO);
}
