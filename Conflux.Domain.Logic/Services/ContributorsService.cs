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
/// Represents the contributors service
/// </summary>
public class ContributorsService
{
    private readonly ConfluxContext _context;

    public ContributorsService(ConfluxContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all contributors whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="query">The string to search in the title or description</param>
    /// <returns>Filtered list of contributors</returns>
    public async Task<List<Contributor>> GetContributorsByQueryAsync(string? query)
    {
        var people = _context.Contributors
            .Include(c => c.Roles)
            .AsQueryable();

        if (string.IsNullOrWhiteSpace(query)) return await people.ToListAsync();

        string loweredQuery = query.ToLowerInvariant();
#pragma warning disable CA1862 // CultureInfo.IgnoreCase cannot by converted to a SQL query, hence we ignore this warning
        people = people.Where(person =>
            person.Name.ToLower().Contains(loweredQuery));
#pragma warning restore CA1862
        return await people.ToListAsync();
    }

    /// <summary>
    /// Gets the contributor by their GUID
    /// </summary>
    /// <param name="id">The GUID of the contributor</param>
    /// <returns>The contributor</returns>
    /// <exception cref="ContributorNotFoundException">Thrown when the contributor is not found</exception>
    public async Task<Contributor> GetContributorByIdAsync(Guid id) =>
        await _context.Contributors
            .Include(c => c.Roles)
            .SingleOrDefaultAsync(p => p.Id == id) ?? throw new ContributorNotFoundException(id);

    /// <summary>
    /// Creates a new contributor
    /// </summary>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The created contributor</returns>
    public async Task<Contributor> CreateContributorAsync(ContributorPostDTO contributorDTO)
    {
        Contributor person = contributorDTO.ToContributor();
        _context.Contributors.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }

    /// <summary>
    /// Updates a contributor via PUT
    /// </summary>
    /// <param name="id">The GUID of the contributor</param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    public async Task<Contributor> UpdateContributorAsync(Guid id, ContributorPutDTO contributorDTO)
    {
        Contributor person = await GetContributorByIdAsync(id);
        person.Name = contributorDTO.Name;
        await _context.SaveChangesAsync();
        return person;
    }

    /// <summary>
    /// Patches a contributor
    /// </summary>
    /// <param name="id">The GUID of the contributor</param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    public async Task<Contributor> PatchContributorAsync(Guid id, ContributorPatchDTO contributorDTO)
    {
        Contributor person = await GetContributorByIdAsync(id);
        person.Name = contributorDTO.Name ?? person.Name;
        await _context.SaveChangesAsync();
        return person;
    }
}
