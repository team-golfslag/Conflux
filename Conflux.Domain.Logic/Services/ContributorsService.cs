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
public class ContributorsService : IContributorsService
{
    private readonly ConfluxContext _context;

    public ContributorsService(ConfluxContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Updates a contributor via PUT
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    public async Task<Contributor> UpdateContributorAsync(Guid projectId, Guid personId, ContributorDTO contributorDTO)
    {
        Contributor contributor = await GetContributorByIdAsync(projectId, personId);
        contributor.Roles = contributorDTO.Roles.ConvertAll(r => new ContributorRole
        {
            PersonId = personId,
            ProjectId = projectId,
            RoleType = r,
        });

        contributor.Positions = contributorDTO.Positions.ConvertAll(p => new ContributorPosition
        {
            PersonId = personId,
            ProjectId = projectId,
            Position = p.Type,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
        });

        contributor.Contact = contributorDTO.Contact;
        contributor.Leader = contributorDTO.Leader;

        await _context.SaveChangesAsync();
        return contributor;
    }

    /// <summary>
    /// Patches a contributor
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    public async Task<Contributor> PatchContributorAsync(Guid projectId, Guid personId,
        ContributorPatchDTO contributorDTO)
    {
        Contributor contributor = await GetContributorByIdAsync(projectId, personId);
        // contributor.Roles = contributorDTO.Roles; TODO make this work
        await _context.SaveChangesAsync();
        return contributor;
    }

    /// <summary>
    /// Gets the contributor by their GUID
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    /// <returns>The contributor</returns>
    /// <exception cref="ContributorNotFoundException">Thrown when the contributor is not found</exception>
    public async Task<Contributor> GetContributorByIdAsync(Guid projectId, Guid personId) =>
        await _context.Contributors
            .Include(c => c.Roles)
            .SingleOrDefaultAsync(p => p.ProjectId == projectId && p.PersonId == personId) ??
        throw new ContributorNotFoundException(projectId);

    /// <summary>
    /// Creates a new contributor
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The created contributor</returns>
    public async Task<Contributor> CreateContributorAsync(Guid projectId, ContributorDTO contributorDTO)
    {
        Contributor person = contributorDTO.ToContributor(projectId, Guid.NewGuid());
        _context.Contributors.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }

    /// <summary>
    /// Gets all contributors whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="query">The string to search in the title or description</param>
    /// <returns>Filtered list of contributors</returns>
    public async Task<List<Contributor>> GetContributorsByQueryAsync(Guid projectId, string? query)
    {
        Project project = await _context.Projects
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Roles)
            .SingleOrDefaultAsync(p => p.Id == projectId) ?? throw new ProjectNotFoundException(projectId);

        var contributors = project.Contributors;

        if (string.IsNullOrWhiteSpace(query)) return contributors;

        string loweredQuery = query.ToLowerInvariant();

#pragma warning disable CA1862 // CultureInfo.IgnoreCase cannot by converted to a SQL query, hence we ignore this warning
        var matchingPersonIds = await _context.People
            .Where(p => p.Name.Contains(loweredQuery))
            .Select(p => p.Id)
            .ToListAsync();

        contributors = contributors.Where(contributor =>
            matchingPersonIds.Contains(contributor.PersonId)).ToList();
#pragma warning restore CA1862
        return contributors;
    }
}
