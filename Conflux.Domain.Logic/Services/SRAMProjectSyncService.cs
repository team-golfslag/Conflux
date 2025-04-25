// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Session;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.Models;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

public class SRAMProjectSyncService : ISRAMProjectSyncService
{
    private readonly ConfluxContext _confluxContext;
    private readonly SCIMApiClient _scimApiClient;

    public SRAMProjectSyncService(SCIMApiClient scimApiClient, ConfluxContext confluxContext)
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
        var roles = project.Users.SelectMany(p => p.Roles).DistinctBy(p => p.Id);
        foreach (UserRole role in roles)
            await SyncProjectRoleAsync(project, role);

        // Update project in database
        _confluxContext.Projects.Update(project);
        await _confluxContext.SaveChangesAsync();
    }

    public async Task SyncProjectRoleAsync(Project project, UserRole userRole)
    {
        SCIMGroup? updatedScimGroup = await _scimApiClient.GetSCIMGroup(project.SCIMId ?? string.Empty);
        if (updatedScimGroup == null) throw new ProjectNotFoundException(project.Id);

        Group updatedGroup = CollaborationMapper.MapSCIMGroup(userRole.Urn, updatedScimGroup);

        // remove role from all users
        foreach (User user in project.Users)
            user.Roles.Remove(userRole);

        UserRole newUserRole = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = updatedGroup.DisplayName,
            Description = updatedGroup.Description,
            Urn = updatedGroup.Urn,
            SCIMId = updatedGroup.SCIMId,
        };

        foreach (User user in project.Users.Where(u => updatedGroup.Members.Any(m => m.SCIMId == u.SCIMId)))
            user.Roles.Add(newUserRole);
    }
}
