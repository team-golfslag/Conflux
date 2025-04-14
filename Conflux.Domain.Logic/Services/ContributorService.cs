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
public class ContributorService
{
    private readonly ConfluxContext _context;

    public ContributorService(ConfluxContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all people whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="query">The string to search in the title or description</param>
    /// <returns>Filtered list of contributors</returns>
    public async Task<List<Contributor>> GetContributorsByQueryAsync(string? query)
    {
        IQueryable<Contributor> contributors = _context.Contributors
            .Include(p => p.Roles);

        if (string.IsNullOrWhiteSpace(query)) return await contributors.ToListAsync();

        string loweredQuery = query.ToLowerInvariant();
#pragma warning disable CA1862 // CultureInfo.IgnoreCase cannot by converted to a SQL query, hence we ignore this warning
        contributors = contributors.Where(person =>
            person.Name.ToLower().Contains(loweredQuery));
#pragma warning restore CA1862
        return await contributors.ToListAsync();
    }

    /// <summary>
    /// Gets the person by their GUID
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <returns>The person</returns>
    /// <exception cref="ContributorNotFoundException">Thrown when the person is not found</exception>
    public async Task<Contributor> GetContributorByIdAsync(Guid id) =>
        await _context.Contributors
            .Include(p => p.Roles)
            .SingleOrDefaultAsync(p => p.Id == id) ?? throw new ContributorNotFoundException(id);

    /// <summary>
    /// Creates a new person
    /// </summary>
    /// <param name="contributorDto">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The created person</returns>
    public async Task<Contributor> CreateContributorAsync(ContributorPostDto contributorDto)
    {
        Contributor contributor = contributorDto.ToContributor();
        _context.Contributors.Add(contributor);
        await _context.SaveChangesAsync();
        return contributor;
    }

    /// <summary>
    /// Updates a person via PUT
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <param name="contributorDto">The DTO which to convert to a <see cref="Contributor" /></param>
    public async Task<Contributor> UpdateContributorAsync(Guid id, ContributorPutDto contributorDto)
    {
        Contributor contributor = await GetContributorByIdAsync(id);
        contributor.Name = contributorDto.Name;
        await _context.SaveChangesAsync();
        return contributor;
    }

    /// <summary>
    /// Patches a person
    /// </summary>
    /// <param name="id">The GUID of the person</param>
    /// <param name="contributorDto">The DTO which to convert to a <see cref="Contributor" /></param>
    public async Task<Contributor> PatchContributorAsync(Guid id, ContributorPatchDto contributorDto)
    {
        Contributor contributor = await GetContributorByIdAsync(id);
        contributor.Name = contributorDto.Name ?? contributor.Name;
        await _context.SaveChangesAsync();
        return contributor;
    }
}
