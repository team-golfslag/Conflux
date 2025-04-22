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
/// Represents the controller for managing people
/// </summary>
[Route("contributors")]
[ApiController]
public class ContributorsController : ControllerBase
{
    private readonly ContributorsService _contributorsService;

    public ContributorsController(ConfluxContext context)
    {
        _contributorsService = new(context);
    }

    /// <summary>
    /// Gets all people whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="query">Optional: The string to search in the title or description</param>
    /// <returns>Filtered list of people</returns>
    [HttpGet]
    public async Task<ActionResult<List<Contributor>>> GetProjectByQuery(
        [FromQuery] string? query) =>
        await _contributorsService.GetContributorsByQueryAsync(query);

    /// <summary>
    /// Gets the person by his GUID
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <returns>The request response</returns>
    [HttpGet]
    [Route("{id:guid}")]
    public async Task<ActionResult<Contributor>> GetPersonByIdAsync([FromRoute] Guid id) =>
        await _contributorsService.GetContributorByIdAsync(id);

    /// <summary>
    /// Creates a new person
    /// </summary>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPost]
    public async Task<ActionResult<Contributor>> CreatePerson([FromBody] ContributorPostDTO contributorDTO) =>
        await _contributorsService.CreateContributorAsync(contributorDTO);

    /// <summary>
    /// Updates a person via PUT
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{id:guid}")]
    public async Task<ActionResult<Contributor>> UpdatePerson([FromRoute] Guid id,
        [FromBody] ContributorPutDTO contributorDTO) =>
        await _contributorsService.UpdateContributorAsync(id, contributorDTO);

    [HttpPatch]
    [Route("{id:guid}")]
    public async Task<ActionResult<Contributor>>
        PatchPerson([FromRoute] Guid id, [FromBody] ContributorPatchDTO contributorDTO) =>
        await _contributorsService.PatchContributorAsync(id, contributorDTO);
}
