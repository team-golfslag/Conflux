// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Request;
using Conflux.Domain.Logic.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

public class PeopleService : IPeopleService
{
    private readonly ConfluxContext _context;

    public PeopleService(ConfluxContext context)
    {
        _context = context;
    }

    public async Task<List<Person>> GetPersonsByQueryAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return await _context.People.ToListAsync();

        string loweredQuery = query.ToLowerInvariant();

        return await _context.People
            .Where(p => p.Name.ToLower().Contains(loweredQuery) ||
                p.GivenName != null && p.GivenName.ToLower().Contains(loweredQuery) ||
                p.FamilyName != null && p.FamilyName.ToLower().Contains(loweredQuery) ||
                p.Email != null && p.Email.ToLower().Contains(loweredQuery))
            .ToListAsync();
    }

    public async Task<Person> GetPersonByIdAsync(Guid id) =>
        await _context.People.FindAsync(id) ??
        throw new PersonNotFoundException(id);

    public async Task<Person> CreatePersonAsync(PersonRequestDTO personDTO)
    {
        Person person = personDTO.ToPerson();
        _context.People.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }

    public async Task<Person> UpdatePersonAsync(Guid id, PersonRequestDTO personDTO)
    {
        Person person = await GetPersonByIdAsync(id);

        person.Name = personDTO.Name;
        person.GivenName = personDTO.GivenName;
        person.FamilyName = personDTO.FamilyName;
        person.Email = personDTO.Email;

        await _context.SaveChangesAsync();
        return person;
    }

    public async Task DeletePersonAsync(Guid id)
    {
        Person person = await GetPersonByIdAsync(id);

        // Check if person is associated with any contributors
        bool hasContributors = await _context.Contributors
            .AnyAsync(c => c.PersonId == id);

        if (hasContributors)
            throw new PersonHasContributorsException(id);

        _context.People.Remove(person);
        await _context.SaveChangesAsync();
    }
}
