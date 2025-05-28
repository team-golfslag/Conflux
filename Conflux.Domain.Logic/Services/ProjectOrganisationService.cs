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
/// Service for managing project organisations
/// </summary>
public class ProjectOrganisationsService : IProjectOrganisationsService
{
    private readonly ConfluxContext _context;
    private readonly IProjectsService _projectsService;

    public ProjectOrganisationsService(ConfluxContext context, IProjectsService projectsService)
    {
        _context = context;
        _projectsService = projectsService;
    }

    /// <inheritdoc />
    public async Task<List<ProjectOrganisationResponseDTO>> GetOrganisationsByProjectIdAsync(Guid projectId)
    {
        // Verify project exists
        bool projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            throw new ProjectNotFoundException(projectId);

        List<ProjectOrganisation> projectOrganisations = await _context.ProjectOrganisations
            .Where(po => po.ProjectId == projectId)
            .Include(po => po.Roles)
            .ToListAsync();

        List<Guid> orgIds = projectOrganisations.Select(po => po.OrganisationId).ToList();
        Dictionary<Guid, Organisation> organisations = await _context.Organisations
            .Where(o => orgIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id);

        // Get project for consistent loading
        ProjectResponseDTO project = await _projectsService.GetProjectDTOByIdAsync(projectId);

        return projectOrganisations.Select(po =>
        {
            // Get the organization for this specific project-organisation relationship
            organisations.TryGetValue(po.OrganisationId, out Organisation? org);

            if (org == null)
                throw new OrganisationNotFoundException(po.OrganisationId);

            return new ProjectOrganisationResponseDTO
            {
                Project = project,
                Organisation = new()
                {
                    Id = org.Id,
                    RORId = org.RORId,
                    Name = org.Name,
                },
                Name = org.Name,
                RORId = org.RORId,
                Roles = po.Roles.Select(r => r.Role).ToList(),
            };
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<ProjectOrganisationResponseDTO> GetOrganisationByIdAsync(Guid projectId, Guid organisationId)
    {
        ProjectOrganisation projectOrganisation = await GetProjectOrganisationEntityAsync(projectId, organisationId);

        Organisation? organisation = await _context.Organisations.FindAsync(organisationId);
        if (organisation == null)
            throw new OrganisationNotFoundException(organisationId);

        return new()
        {
            Project = await _projectsService.GetProjectDTOByIdAsync(projectId),
            Organisation = new()
            {
                Id = organisation.Id,
                RORId = organisation.RORId,
                Name = organisation.Name,
            },
            Name = organisation.Name,
            RORId = organisation.RORId,
            Roles = projectOrganisation.Roles.Select(r => r.Role).ToList(),
        };
    }

    /// <inheritdoc />
    public async Task<ProjectOrganisationResponseDTO> CreateOrganisationAsync(Guid projectId,
        OrganisationRequestDTO organisationDto)
    {
        // Verify project exists
        bool projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            throw new ProjectNotFoundException(projectId);

        // Check if organisation exists, if not create it
        Organisation? organisation = await _context.Organisations
            .FirstOrDefaultAsync(o => o.RORId == organisationDto.RORId);

        if (organisation == null)
        {
            organisation = new()
            {
                Id = Guid.NewGuid(),
                Name = organisationDto.Name,
                RORId = organisationDto.RORId ?? string.Empty,
            };
            _context.Organisations.Add(organisation);
        }

        // Check if the project already has this organisation
        bool alreadyExists = await _context.ProjectOrganisations
            .AnyAsync(po => po.ProjectId == projectId && po.OrganisationId == organisation.Id);
        if (alreadyExists)
            throw new ProjectAlreadyHasOrganisationException(projectId, organisation.Id);

        // Create the project organisation
        ProjectOrganisation projectOrganisation = new()
        {
            ProjectId = projectId,
            OrganisationId = organisation.Id,
            Roles = organisationDto.Roles,
        };

        _context.ProjectOrganisations.Add(projectOrganisation);
        await _context.SaveChangesAsync();

        return new()
        {
            Project = await _projectsService.GetProjectDTOByIdAsync(projectId),
            Organisation = new()
            {
                Id = organisation.Id,
                RORId = organisation.RORId,
                Name = organisation.Name,
            },
            Name = organisation.Name,
            RORId = organisation.RORId,
            Roles = projectOrganisation.Roles.Select(r => r.Role).ToList(),
        };
    }

    /// <inheritdoc />
    public async Task<ProjectOrganisationResponseDTO> UpdateOrganisationAsync(Guid projectId, Guid organisationId,
        OrganisationRequestDTO organisationDto)
    {
        // Get the organisation
        Organisation? organisation = await _context.Organisations.FindAsync(organisationId);
        if (organisation == null)
            throw new OrganisationNotFoundException(organisationId);

        // Update organisation details
        organisation.Name = organisationDto.Name;
        organisation.RORId = organisationDto.RORId ?? string.Empty;

        // Update roles - first remove existing ones
        await _context.OrganisationRoles
            .Where(r => r.ProjectId == projectId && r.OrganisationId == organisationId)
            .ExecuteDeleteAsync();

        // Add new roles
        _context.OrganisationRoles.AddRange(organisationDto.Roles);

        await _context.SaveChangesAsync();

        // Reload roles
        List<OrganisationRole> updatedRoles = await _context.OrganisationRoles
            .Where(r => r.ProjectId == projectId && r.OrganisationId == organisationId)
            .ToListAsync();

        return new()
        {
            Project = await _projectsService.GetProjectDTOByIdAsync(projectId),
            Organisation = new()
            {
                Id = organisation.Id,
                RORId = organisation.RORId ?? string.Empty,
                Name = organisation.Name,
            },
            Name = organisation.Name,
            RORId = organisation.RORId ?? string.Empty,
            Roles = updatedRoles.Select(r => r.Role).ToList(),
        };
    }

    /// <inheritdoc />
    public async Task DeleteOrganisationAsync(Guid projectId, Guid organisationId)
    {
        // Verify the relationship exists
        ProjectOrganisation projectOrganisation = await GetProjectOrganisationEntityAsync(projectId, organisationId);

        // Delete all related roles first
        await _context.OrganisationRoles
            .Where(r => r.ProjectId == projectId && r.OrganisationId == organisationId)
            .ExecuteDeleteAsync();

        // Delete the project-organisation relationship
        _context.ProjectOrganisations.Remove(projectOrganisation);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Helper method to get a project organisation entity
    /// </summary>
    private async Task<ProjectOrganisation> GetProjectOrganisationEntityAsync(Guid projectId, Guid organisationId)
    {
        ProjectOrganisation? projectOrganisation = await _context.ProjectOrganisations
            .Include(po => po.Roles)
            .SingleOrDefaultAsync(po => po.ProjectId == projectId && po.OrganisationId == organisationId);

        if (projectOrganisation == null)
            throw new ProjectOrganisationNotFoundException(projectId, organisationId);

        return projectOrganisation;
    }
}
