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
    public async Task<ContributorDTO> UpdateContributorAsync(Guid projectId, Guid personId,
        ContributorDTO contributorDTO)
    {
        Contributor contributor = await GetContributorEntityAsync(projectId, personId);
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

        return await MapToContributorDTOAsync(contributor);
    }

    /// <summary>
    /// Patches a contributor
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    /// <param name="contributorDTO">The DTO which to partially update a <see cref="Contributor" /></param>
    public async Task<ContributorDTO> PatchContributorAsync(Guid projectId, Guid personId,
        ContributorPatchDTO contributorDTO)
    {
        Contributor contributor = await GetContributorEntityAsync(projectId, personId);

        if (contributorDTO.Roles != null)
            contributor.Roles = contributorDTO.Roles.ConvertAll(r => new ContributorRole
            {
                PersonId = personId,
                ProjectId = projectId,
                RoleType = r,
            });

        if (contributorDTO.Positions != null)
            contributor.Positions = contributorDTO.Positions.ConvertAll(p => new ContributorPosition
            {
                PersonId = personId,
                ProjectId = projectId,
                Position = p.Type,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
            });

        if (contributorDTO.Leader.HasValue) contributor.Leader = contributorDTO.Leader.Value;

        if (contributorDTO.Contact.HasValue) contributor.Contact = contributorDTO.Contact.Value;

        await _context.SaveChangesAsync();

        return await MapToContributorDTOAsync(contributor);
    }

    public Task DeleteContributorAsync(Guid projectId, Guid personId)
    {
        Contributor contributor = _context.Contributors
                .Include(c => c.Roles)
                .Include(c => c.Positions)
                .SingleOrDefault(p => p.ProjectId == projectId && p.PersonId == personId) ??
            throw new ContributorNotFoundException(projectId);

        _context.Contributors.Remove(contributor);
        return _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the contributor by their GUID
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    /// <returns>The contributor DTO with person data</returns>
    /// <exception cref="ContributorNotFoundException">Thrown when the contributor is not found</exception>
    public async Task<ContributorDTO> GetContributorByIdAsync(Guid projectId, Guid personId)
    {
        Contributor contributor = await GetContributorEntityAsync(projectId, personId);
        return await MapToContributorDTOAsync(contributor);
    }

    /// <summary>
    /// Creates a new contributor
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The created contributor DTO with person data</returns>
    public async Task<ContributorDTO> CreateContributorAsync(ContributorDTO contributorDTO)
    {
        // check if the contributor already exists
        Contributor? existingContributor = await _context.Contributors
            .FindAsync(contributorDTO.Person.Id, contributorDTO.ProjectId);
        if (existingContributor != null)
            throw new ContributorAlreadyExistsException(contributorDTO.ProjectId, contributorDTO.Person.Id);

        Contributor contributor = contributorDTO.ToContributor();
        _context.Contributors.Add(contributor);
        await _context.SaveChangesAsync();

        return await MapToContributorDTOAsync(contributor);
    }

    /// <summary>
    /// Gets all contributors whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="query">The string to search in the title or description</param>
    /// <returns>Filtered list of contributor DTOs with person data</returns>
    public async Task<List<ContributorDTO>> GetContributorsByQueryAsync(Guid projectId, string? query)
    {
        var contributors = _context.Contributors
            .Include(c => c.Roles)
            .Include(c => c.Positions)
            .Where(c => c.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(query))
        {
            string loweredQuery = query.ToLowerInvariant();
            var matchingPersonIds = await _context.People
                .Where(p => p.Name.ToLower().Contains(loweredQuery))
                .Select(p => p.Id)
                .ToListAsync();

            contributors = contributors.Where(c => matchingPersonIds.Contains(c.PersonId));
        }

        var contributorList = await contributors.ToListAsync();
        if (contributorList.Count == 0)
            return [];

        // Fetch all the relevant persons in one go to avoid N+1 query problem
        var personIds = contributorList.Select(c => c.PersonId).Distinct().ToList();
        var persons = await _context.People
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // Map to DTOs with person data
        return contributorList.Select(c => new ContributorDTO
        {
            Person = persons.TryGetValue(c.PersonId, out Person? person) ? person : null,
            ProjectId = projectId,
            Roles = c.Roles.Select(r => r.RoleType).ToList(),
            Positions = c.Positions.Select(p => new ContributorPositionDTO
            {
                Type = p.Position,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
            }).ToList(),
            Leader = c.Leader,
            Contact = c.Contact,
        }).ToList();
    }

    /// <summary>
    /// Helper method to get a contributor entity
    /// </summary>
    private async Task<Contributor> GetContributorEntityAsync(Guid projectId, Guid personId) =>
        await _context.Contributors
            .Include(c => c.Roles)
            .Include(c => c.Positions)
            .SingleOrDefaultAsync(p => p.ProjectId == projectId && p.PersonId == personId) ??
        throw new ContributorNotFoundException(projectId);

    /// <summary>
    /// Maps a contributor entity to a contributor DTO with person data
    /// </summary>
    private async Task<ContributorDTO> MapToContributorDTOAsync(Contributor contributor)
    {
        Person? person = await _context.People.FindAsync(contributor.PersonId);

        return new()
        {
            Person = person,
            ProjectId = contributor.ProjectId,
            Roles = contributor.Roles.Select(r => r.RoleType).ToList(),
            Positions = contributor.Positions.Select(p => new ContributorPositionDTO
            {
                Type = p.Position,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
            }).ToList(),
            Leader = contributor.Leader,
            Contact = contributor.Contact,
        };
    }
}
