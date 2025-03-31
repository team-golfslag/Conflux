using Conflux.API.Services;
using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for managing people
/// </summary>
[Route("person")]
[ApiController]
public class PersonController : ControllerBase
{
    private readonly PersonService _personService;

    public PersonController(ConfluxContext context)
    {
        _personService = new(context);
    }

    /// <summary>
    /// Gets the person by his GUID
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <returns>The request response</returns>
    [HttpGet]
    [Route("{id:guid}")]
    public async Task<ActionResult<Person>> GetPersonByIdAsync([FromRoute] Guid id) =>
        Ok(await _personService.GetPersonByIdAsync(id));

    /// <summary>
    /// Creates a new person
    /// </summary>
    /// <param name="personDto">The DTO which to convert to a <see cref="Person" /></param>
    /// <returns>The request response</returns>
    [HttpPost]
    [Route("create")]
    public async Task<ActionResult<Person>> CreatePerson([FromBody] PersonDTO personDto) =>
        await _personService.CreatePersonAsync(personDto);
}
