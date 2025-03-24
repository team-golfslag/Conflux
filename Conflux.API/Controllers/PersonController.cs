using Conflux.API.DTOs;
using Conflux.API.Services;
using Conflux.Data;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conflux.API.Controllers;

/// <summary>
/// Represents the controller for managing projects
/// </summary>
[Route("person")]
[ApiController]
public class PersonController : ControllerBase
{
    private readonly ConfluxContext _context;

    public PersonController(ConfluxContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Gets a person by its GUID.
    /// </summary>
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
    public ActionResult CreateProject([FromBody] PersonDto personDto)
    {
        Person person = personDto.ToPerson();
        _context.People.Add(person);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetPersonById), new { id = person.Id }, person);
    }
}
