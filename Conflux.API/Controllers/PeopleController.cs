// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

[ApiController]
[Route("people")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class PeopleController : ControllerBase
{
    private readonly IPeopleService _peopleService;

    public PeopleController(IPeopleService peopleService)
    {
        _peopleService = peopleService;
    }

    /// <summary>
    /// Gets all people whose name, given name, family name, or email contains the query (case-insensitive)
    /// </summary>
    /// <param name="query">Optional: The string to search in person fields</param>
    /// <returns>Filtered list of people</returns>
    [HttpGet("query")]
    [ProducesResponseType(typeof(List<Person>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Person>>> GetPersonsByQuery([FromQuery] string? query) =>
        await _peopleService.GetPersonsByQueryAsync(query);

    /// <summary>
    /// Gets a person by their ID
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <returns>The person with the specified ID</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Person), StatusCodes.Status200OK)]
    public async Task<ActionResult<Person>> GetPersonById([FromRoute] Guid id) =>
        await _peopleService.GetPersonByIdAsync(id);

    /// <summary>
    /// Creates a new person
    /// </summary>
    /// <param name="personDTO">The person data</param>
    /// <returns>The created person</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Person), StatusCodes.Status201Created)]
    public async Task<ActionResult<Person>> CreatePerson([FromBody] PersonDTO personDTO)
    {
        Person person = await _peopleService.CreatePersonAsync(personDTO);
        return CreatedAtAction(nameof(GetPersonById), new
        {
            id = person.Id,
        }, person);
    }

    /// <summary>
    /// Updates a person via PUT
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <param name="personDTO">The updated person data</param>
    /// <returns>The updated person</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Person), StatusCodes.Status200OK)]
    public async Task<ActionResult<Person>> UpdatePerson([FromRoute] Guid id, [FromBody] PersonDTO personDTO) =>
        await _peopleService.UpdatePersonAsync(id, personDTO);

    /// <summary>
    /// Updates a person via PATCH
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <param name="personPatchDTO">The partial person data to update</param>
    /// <returns>The updated person</returns>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(Person), StatusCodes.Status200OK)]
    public async Task<ActionResult<Person>>
        PatchPerson([FromRoute] Guid id, [FromBody] PersonPatchDTO personPatchDTO) =>
        await _peopleService.PatchPersonAsync(id, personPatchDTO);

    /// <summary>
    /// Deletes a person
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeletePerson([FromRoute] Guid id)
    {
        await _peopleService.DeletePersonAsync(id);
        return NoContent();
    }
}
