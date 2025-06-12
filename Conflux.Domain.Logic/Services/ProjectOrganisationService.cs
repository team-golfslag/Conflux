// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ROR.Net;
using ROR.Net.Models;
using ROR.Net.Services;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// Service for managing project organisations
/// </summary>
public class ProjectOrganisationsService : IProjectOrganisationsService
{
    private readonly ConfluxContext _context;
    private readonly IProjectsService _projectsService;
    private readonly IOrganizationService _organizationService;

    public ProjectOrganisationsService(ConfluxContext context, IProjectsService projectsService, IOrganizationService organizationService)
    {
        _context = context;
        _projectsService = projectsService;
        _organizationService = organizationService;
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
                ProjectId = projectId,
                Organisation = new()
                {
                    Id = org.Id,
                    RORId = org.RORId,
                    Name = org.Name,
                    Roles = po.Roles.Select(r => new OrganisationRoleResponseDTO
                    {
                        Role = r.Role,
                        StartDate = r.StartDate,
                        EndDate = r.EndDate,
                    }).ToList(),
                },
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
            ProjectId = projectId,
            Organisation = new()
            {
                Id = organisation.Id,
                RORId = organisation.RORId,
                Name = organisation.Name,
                Roles = projectOrganisation.Roles.Select(r => new OrganisationRoleResponseDTO
                {
                    Role = r.Role,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                }).ToList(),
            },
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
        Organisation? organisation = string.IsNullOrEmpty(organisationDto.RORId) ? null : await _context.Organisations
            .FirstOrDefaultAsync(o => o.RORId == organisationDto.RORId);

        if (organisation == null)
        {
            organisation = new()
            {
                Id = Guid.CreateVersion7(),
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
        
        if (organisationDto.Role == null)
            throw new ProjectOrganisationException("Role cannot be null");

        // Create the project organisation
        ProjectOrganisation projectOrganisation = new()
        {
            ProjectId = projectId,
            OrganisationId = organisation.Id,
            Roles = [new OrganisationRole
            {
                Role = (OrganisationRoleType)organisationDto.Role,
                StartDate = DateTime.UtcNow.Date,
            }],
        };

        _context.ProjectOrganisations.Add(projectOrganisation);
        _context.OrganisationRoles.AddRange(projectOrganisation.Roles);
        await _context.SaveChangesAsync();

        return new()
        {
            ProjectId = projectId,
            Organisation = new()
            {
                Id = organisation.Id,
                RORId = organisation.RORId,
                Name = organisation.Name,
                Roles = projectOrganisation.Roles.Select(r => new OrganisationRoleResponseDTO
                {
                    Role = r.Role,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                }).ToList(),
            },
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
        OrganisationRole? currentRole = await _context.OrganisationRoles
            .Where(r => r.ProjectId == projectId && r.OrganisationId == organisationId && r.EndDate == null)
            .SingleOrDefaultAsync();

        if (currentRole != null && (!organisationDto.Role.HasValue || organisationDto.Role.HasValue && currentRole.Role != organisationDto.Role.Value))
        {
            // Set the end date for the current role
            currentRole.EndDate = DateTime.UtcNow.Date;
            _context.OrganisationRoles.Update(currentRole);
        }
        
        if (organisationDto.Role.HasValue && (currentRole == null || currentRole.Role != organisationDto.Role.Value))
        {
            // Create a new role with the new role type
            OrganisationRole newRole = new()
            {
                ProjectId = projectId,
                OrganisationId = organisationId,
                Role = organisationDto.Role.Value,
                StartDate = DateTime.UtcNow.Date,
            };
            _context.OrganisationRoles.Add(newRole);
        }

        await _context.SaveChangesAsync();

        // Reload roles
        List<OrganisationRole> updatedRoles = await _context.OrganisationRoles
            .Where(r => r.ProjectId == projectId && r.OrganisationId == organisationId)
            .ToListAsync();

        return new()
        {
            ProjectId = projectId,
            Organisation = new()
            {
                Id = organisation.Id,
                RORId = organisation.RORId ?? string.Empty,
                Name = organisation.Name,
                Roles = updatedRoles.Select(r => new OrganisationRoleResponseDTO
                {
                    Role = r.Role,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                }).ToList(),
            },
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

    public async Task<OrganisationResponseDTO> GetOrganisationNameByRorAsync(string ror)
    {
        Organization? org = await _organizationService.GetOrganizationAsync(ror);
        if (org == null) throw new OrganisationNotFoundException($"Organisation with ROR ID {ror} not found.");
        

        return new OrganisationResponseDTO()
        {
            Name = org.Name,
            RORId = org.Id,
        };

    }
}
