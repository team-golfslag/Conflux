// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// Represents the person service
/// </summary>
public class PeopleService
{
    private readonly ConfluxContext _context;

    public PeopleService(ConfluxContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all people whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="query">The string to search in the title or description</param>
    /// <returns>Filtered list of people</returns>
    public async Task<List<Person>> GetPeopleByQueryAsync(string? query)
    {
        IQueryable<Person> people = _context.People;

        if (string.IsNullOrWhiteSpace(query)) return await people.ToListAsync();

        string loweredQuery = query.ToLowerInvariant();
#pragma warning disable CA1862 // CultureInfo.IgnoreCase cannot by converted to a SQL query, hence we ignore this warning
        people = people.Where(person =>
            person.Name.ToLower().Contains(loweredQuery));
#pragma warning restore CA1862
        return await people.ToListAsync();
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
    public async Task<Person> CreatePersonAsync(PersonPostDTO personDto)
    {
        Person person = personDto.ToPerson();
        _context.People.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }

    /// <summary>
    /// Updates a person via PUT
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <param name="personDto">The DTO which to convert to a <see cref="Person" /></param>
    public async Task<Person> UpdatePersonAsync(Guid id, PersonPutDTO personDto)
    {
        Person person = await GetPersonByIdAsync(id);
        person.Name = personDto.Name;
        await _context.SaveChangesAsync();
        return person;
    }

    /// <summary>
    /// Patches a person
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <param name="personDto">The DTO which to convert to a <see cref="Person" /></param>
    public async Task<Person> PatchPersonAsync(Guid id, PersonPatchDTO personDto)
    {
        Person person = await GetPersonByIdAsync(id);
        person.Name = personDto.Name ?? person.Name;
        await _context.SaveChangesAsync();
        return person;
    }
}
