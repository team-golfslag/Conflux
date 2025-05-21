// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Session;
using Conflux.Integrations.SRAM;
using Microsoft.EntityFrameworkCore;
using SRAM.SCIM.Net;
using SRAM.SCIM.Net.Exceptions;
using SRAM.SCIM.Net.Models;

namespace Conflux.Domain.Logic.Services;

public class SRAMProjectSyncService : ISRAMProjectSyncService
{
    private readonly ConfluxContext _confluxContext;
    private readonly ISCIMApiClient _scimApiClient;

    public SRAMProjectSyncService(ISCIMApiClient scimApiClient, ConfluxContext confluxContext)
    {
        _scimApiClient = scimApiClient;
        _confluxContext = confluxContext;
    }

    /// <summary>
    /// Checks for updates to the projects or its members
    /// </summary>
    /// <param name="projectId"></param>
    /// <exception cref="ProjectNotFoundException"></exception>
    /// <remarks>Only syncs existing roles</remarks>
    public async Task SyncProjectAsync(Guid projectId)
    {
        // Retrieve the project from the database
        Project project = await _confluxContext.Projects
            .Include(p => p.Users)
            .ThenInclude(p => p.Roles)
            .SingleOrDefaultAsync(p => p.Id == projectId) ?? throw new ProjectNotFoundException(projectId);

        // Retrieve the project from the API
        SCIMGroup? apiProject = await _scimApiClient.GetSCIMGroup(project.SCIMId ?? string.Empty);
        if (apiProject == null) throw new ProjectNotFoundException(projectId);

        Group projectGroup = CollaborationMapper.MapSCIMGroup("", apiProject);

        // Add new members
        var newUsers = projectGroup.Members
            .Where(m => project.Users.All(u => m.SCIMId != u.SCIMId))
            .Select(m => new User
            {
                Id = Guid.NewGuid(),
                SRAMId = null,
                SCIMId = m.SCIMId,
                ORCiD = null,
                Name = m.DisplayName,
                Roles = [],
                GivenName = null,
                FamilyName = null,
                Email = null,
            });

        project.Users.AddRange(newUsers);

        // Get all roles 
        var roles = await _confluxContext.UserRoles
            .Where(r => r.ProjectId == project.Id)
            .ToListAsync();
        foreach (UserRole role in roles)
            await SyncProjectRoleAsync(project, role);

        // Update project in database
        _confluxContext.Projects.Update(project);
        await _confluxContext.SaveChangesAsync();
    }

    public async Task SyncProjectRoleAsync(Project project, UserRole userRole)
    {
        SCIMGroup? updatedScimGroup = await _scimApiClient.GetSCIMGroup(userRole.SCIMId);
        if (updatedScimGroup == null) throw new GroupNotFoundException(userRole.Urn);

        Group updatedGroup = CollaborationMapper.MapSCIMGroup(userRole.Urn, updatedScimGroup);

        // remove role from all users
        foreach (User user in project.Users)
            user.Roles.RemoveAll(r => r.ProjectId == project.Id && r.Type == userRole.Type);

        _confluxContext.UserRoles.Remove(userRole);

        UserRole newUserRole = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Type = SessionMappingService.GroupUrnToUserRoleType(updatedGroup.Urn),
            Urn = updatedGroup.Urn,
            SCIMId = updatedGroup.SCIMId,
        };

        // Add the new role to the context first
        _confluxContext.UserRoles.Add(newUserRole);

        // Then associate it with users
        foreach (User user in project.Users.Where(u => updatedGroup.Members.Any(m => m.SCIMId == u.SCIMId)))
            user.Roles.Add(newUserRole);
    }
}
