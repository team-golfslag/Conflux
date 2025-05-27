// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
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
    public async Task<ContributorResponseDTO> UpdateContributorAsync(Guid projectId, Guid personId,
        ContributorRequestDTO contributorDTO)
    {
        Contributor contributor = await GetContributorEntityAsync(projectId, personId);
        contributor.Roles = contributorDTO.Roles.ConvertAll(r => new ContributorRole
        {
            PersonId = personId,
            ProjectId = projectId,
            RoleType = r,
        });

        if (contributorDTO.Position.HasValue)
        {
            ContributorPosition? currentActivePosition = contributor.Positions
                .FirstOrDefault(p => p.EndDate == null);

            if (currentActivePosition != null)
                currentActivePosition.EndDate = DateTime.UtcNow.Date;

            if (currentActivePosition == null || currentActivePosition.Position != contributorDTO.Position)
                contributor.Positions.Add(new()
                {
                    PersonId = personId,
                    ProjectId = projectId,
                    Position = contributorDTO.Position.Value,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = null,
                });
        }

        contributor.Contact = contributorDTO.Contact;
        contributor.Leader = contributorDTO.Leader;

        await _context.SaveChangesAsync();

        return await MapToContributorDTOAsync(contributor);
    }

    /// <summary>
    /// Gets the contributor by their GUID
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="personId">The GUID of the person</param>
    /// <returns>The contributor DTO with person data</returns>
    /// <exception cref="ContributorNotFoundException">Thrown when the contributor is not found</exception>
    public async Task<ContributorResponseDTO> GetContributorByIdAsync(Guid projectId, Guid personId)
    {
        Contributor contributor = await GetContributorEntityAsync(projectId, personId);
        return await MapToContributorDTOAsync(contributor);
    }

    /// <summary>
    /// Gets all contributors whose name contains the query (case-insensitive)
    /// </summary>
    /// <param name="projectId">The GUID of the project</param>
    /// <param name="query">The string to search in the title or description</param>
    /// <returns>Filtered list of contributor DTOs with person data</returns>
    public async Task<List<ContributorResponseDTO>> GetContributorsByQueryAsync(Guid projectId, string? query)
    {
        IQueryable<Contributor> contributors = _context.Contributors
            .Include(c => c.Roles)
            .Include(c => c.Positions)
            .Where(c => c.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(query))
        {
            string loweredQuery = query.ToLowerInvariant();
            List<Guid> matchingPersonIds = await _context.People
                .Where(p => p.Name.ToLower().Contains(loweredQuery))
                .Select(p => p.Id)
                .ToListAsync();

            contributors = contributors.Where(c => matchingPersonIds.Contains(c.PersonId));
        }

        List<Contributor> contributorList = await contributors.ToListAsync();

        // Fetch all the relevant persons in one go to avoid N+1 query problem
        List<Guid> personIds = contributorList.Select(c => c.PersonId).Distinct().ToList();
        Dictionary<Guid, Person> people = await _context.People
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // Map to DTOs with person data
        return contributorList.Select(c => new ContributorResponseDTO
        {
            Person = people.TryGetValue(c.PersonId, out Person? person)
                ? new()
                {
                    Id = person.Id,
                    ORCiD = person.ORCiD,
                    Name = person.Name,
                    GivenName = person.GivenName,
                    FamilyName = person.FamilyName,
                    Email = person.Email,
                }
                : throw new PersonNotFoundException(c.PersonId),
            Roles = c.Roles.ConvertAll(r => new ContributorRoleResponseDTO
            {
                PersonId = c.PersonId,
                ProjectId = c.ProjectId,
                RoleType = r.RoleType,
            }),
            Positions = c.Positions.ConvertAll(p => new ContributorPositionResponseDTO
            {
                PersonId = c.PersonId,
                ProjectId = c.ProjectId,
                Position = p.Position,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
            }),
            Leader = c.Leader,
            Contact = c.Contact,
        }).ToList();
    }

    /// <summary>
    /// Creates a new contributor
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="contributorDTO">The DTO which to convert to a <see cref="Contributor" /></param>
    /// <returns>The created contributor DTO with person data</returns>
    public async Task<ContributorResponseDTO> CreateContributorAsync(Guid projectId, Guid personId,
        ContributorRequestDTO contributorDTO)
    {
        List<ContributorPosition> positions = contributorDTO.Position == null
            ? []
            : new()
            {
                new()
                {
                    PersonId = personId,
                    ProjectId = projectId,
                    Position = contributorDTO.Position.Value,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = null,
                },
            };

        Contributor contributor = new()
        {
            PersonId = personId,
            ProjectId = projectId,
            Roles = contributorDTO.Roles.ConvertAll(r => new ContributorRole
            {
                PersonId = personId,
                ProjectId = projectId,
                RoleType = r,
            }),
            Positions = positions,
            Leader = contributorDTO.Leader,
            Contact = contributorDTO.Contact,
        };
        _context.Contributors.Add(contributor);
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
    private async Task<ContributorResponseDTO> MapToContributorDTOAsync(Contributor contributor)
    {
        Person person = await _context.People.FindAsync(contributor.PersonId) ??
            throw new PersonNotFoundException(contributor.PersonId);

        return new()
        {
            Person = new()
            {
                Id = person.Id,
                ORCiD = person.ORCiD,
                Name = person.Name,
                GivenName = person.GivenName,
                FamilyName = person.FamilyName,
                Email = person.Email,
            },
            ProjectId = contributor.ProjectId,
            Roles = contributor.Roles.ConvertAll(r => new ContributorRoleResponseDTO
            {
                PersonId = contributor.PersonId,
                ProjectId = contributor.ProjectId,
                RoleType = r.RoleType,
            }),
            Positions = contributor.Positions.ConvertAll(p => new ContributorPositionResponseDTO
            {
                PersonId = contributor.PersonId,
                ProjectId = contributor.ProjectId,
                Position = p.Position,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
            }),
            Leader = contributor.Leader,
            Contact = contributor.Contact,
        };
    }
}
