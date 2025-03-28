using Conflux.API.DTOs;
using Conflux.Data;
using Conflux.Domain;

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
    /// Creates a new person
    /// </summary>
    /// <param name="personDto">The DTO which to convert to a <see cref="Person"/></param>
    /// <returns>The created person</returns>
    public async Task<Person?> CreatePersonAsync(PersonDto personDto)
    {
        Person person = personDto.ToPerson();
        _context.People.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }
}
