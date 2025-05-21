// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
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

    public Task<Person?> GetPersonByOrcidIdAsync(string orcidId)
    {
        if (string.IsNullOrWhiteSpace(orcidId))
            throw new ArgumentException("ORCiD ID cannot be null or empty.", nameof(orcidId));

        return _context.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ORCiD == orcidId);
    }

    public async Task<Person> CreatePersonAsync(PersonDTO personDTO)
    {
        Person person = personDTO.ToPerson();
        _context.People.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }

    public async Task<Person> UpdatePersonAsync(Guid id, PersonDTO personDTO)
    {
        Person person = await GetPersonByIdAsync(id);

        person.Name = personDTO.Name;
        person.GivenName = personDTO.GivenName;
        person.FamilyName = personDTO.FamilyName;
        person.Email = personDTO.Email;

        await _context.SaveChangesAsync();
        return person;
    }

    public async Task<Person> PatchPersonAsync(Guid id, PersonPatchDTO personPatchDTO)
    {
        Person person = await GetPersonByIdAsync(id);

        if (personPatchDTO.Name != null)
            person.Name = personPatchDTO.Name;

        if (personPatchDTO.GivenName != null)
            person.GivenName = personPatchDTO.GivenName;

        if (personPatchDTO.FamilyName != null)
            person.FamilyName = personPatchDTO.FamilyName;

        if (personPatchDTO.Email != null)
            person.Email = personPatchDTO.Email;

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
