using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Conflux.API.Services;

/// <summary>
/// Represents the person service
/// </summary>
public class PersonService
{
    private readonly ConfluxContext _context;

    public PersonService(ConfluxContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the person by their GUID
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <returns>The person</returns>
    /// <exception cref="PersonNotFoundException">Thrown when the person is not found</exception>
    public async Task<Person> GetPersonByIdAsync(Guid id) =>
        await _context.People
            .SingleOrDefaultAsync(p => p.Id == id) ?? throw new PersonNotFoundException(id);

    /// <summary>
    /// Creates a new person
    /// </summary>
    /// <param name="personDto">The DTO which to convert to a <see cref="Person" /></param>
    /// <returns>The created person</returns>
    public async Task<Person> CreatePersonAsync(PersonDTO personDto)
    {
        Person person = personDto.ToPerson();
        _context.People.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }
}
