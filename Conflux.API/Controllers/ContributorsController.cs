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
public class ContributorsController : ControllerBase
{
    private readonly ContributorService _contributorService;

    public ContributorsController(ConfluxContext context)
    {
        _contributorService = new(context);
    }

    /// <summary>
    /// Gets all contributors whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="query">Optional: The string to search in the title or description</param>
    /// <returns>Filtered list of contributors</returns>
    [HttpGet]
    public async Task<ActionResult<List<Project>>> GetPeopleByQuery(
        [FromQuery] string? query) =>
        Ok(await _contributorService.GetContributorsByQueryAsync(query));

    /// <summary>
    /// Gets the contributor by his GUID
    /// </summary>
    /// <param name="id">The GUID of the contributor</param>
    /// <returns>The request response</returns>
    [HttpGet]
    [Route("{id:guid}")]
    public async Task<ActionResult<Contributor>> GetContributorByIdAsync([FromRoute] Guid id) =>
        Ok(await _contributorService.GetContributorByIdAsync(id));

    /// <summary>
    /// Creates a new contributor
    /// </summary>
    /// <param name="contributorDto">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPost]
    public async Task<ActionResult<Contributor>> CreateContributor([FromBody] ContributorPostDto contributorDto) =>
        await _contributorService.CreateContributorAsync(contributorDto);

    /// <summary>
    /// Updates a contributor via PUT
    /// </summary>
    /// <param name="id">The GUID of the contributor</param>
    /// <param name="contributorDto">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{id:guid}")]
    public async Task<ActionResult> UpdateContributor([FromRoute] Guid id, [FromBody] ContributorPutDto contributorDto) =>
        Ok(await _contributorService.UpdateContributorAsync(id, contributorDto));

    [HttpPatch]
    [Route("{id:guid}")]
    public async Task<ActionResult> PatchContributor([FromRoute] Guid id, [FromBody] ContributorPatchDto contributorDto) =>
        Ok(await _contributorService.PatchContributorAsync(id, contributorDto));
}
