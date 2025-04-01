using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for managing people
/// </summary>
[Route("people")]
[ApiController]
public class PeopleController : ControllerBase
{
    private readonly PeopleService _peopleService;

    public PeopleController(ConfluxContext context)
    {
        _peopleService = new(context);
    }

    /// <summary>
    /// Gets the person by his GUID
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <returns>The request response</returns>
    [HttpGet]
    [Route("{id:guid}")]
    public async Task<ActionResult<Person>> GetPersonByIdAsync([FromRoute] Guid id) =>
        Ok(await _peopleService.GetPersonByIdAsync(id));

    /// <summary>
    /// Creates a new person
    /// </summary>
    /// <param name="personDto">The DTO which to convert to a <see cref="Person" /></param>
    /// <returns>The request response</returns>
    [HttpPost]
    public async Task<ActionResult<Person>> CreatePerson([FromBody] PersonPostDTO personDto) =>
        await _peopleService.CreatePersonAsync(personDto);

    /// <summary>
    /// Updates a person via PUT
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <param name="personDto">The DTO which to convert to a <see cref="Person" /></param>
    /// <returns>The request response</returns>
    [HttpPut]
    [Route("{id:guid}")]
    public async Task<ActionResult> UpdatePerson([FromRoute] Guid id, [FromBody] PersonPutDTO personDto) =>
        Ok(await _peopleService.UpdatePersonAsync(id, personDto));

    [HttpPatch]
    [Route("{id:guid}")]
    public async Task<ActionResult> PatchPerson([FromRoute] Guid id, [FromBody] PersonPatchDTO personDto) =>
        Ok(await _peopleService.PatchPersonAsync(id, personDto));
}
