using Conflux.API.DTOs;
using Conflux.API.Services;
using Conflux.Data;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for managing people
/// </summary>
[Route("person")]
[ApiController]
public class PersonController : ControllerBase
{
    private readonly ConfluxContext _context;
    private readonly PersonService _personService;

    public PersonController(ConfluxContext context)
    {
        _context = context;
        _personService = new (_context);
    }
    
    /// <summary>
    /// Gets the person by his GUID
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <returns>The request response</returns>
    [HttpGet]
    [Route("{id:guid}")]
    public ActionResult<Person> GetPersonById([FromRoute] Guid id)
    {
        Person? person = _context.People
            .FirstOrDefault(p => p.Id == id);
        return person == null ? NotFound() : Ok(person);
    } 

    /// <summary>
    /// Creates a new person
    /// </summary>
    /// <param name="personDto">The DTO which to convert to a <see cref="Person"/></param>
    /// <returns>The request response</returns>
    [HttpPost]
    [Route("create")]
    public async Task<ActionResult> CreatePerson([FromBody] PersonDto personDto)
    {
        Person? person = await _personService.CreatePersonAsync(personDto);
        return person == null ? BadRequest() : Ok(person);
    }
}
