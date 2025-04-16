// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.Exceptions;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.Models;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

public class ProjectSyncService : IProjectSyncService
{
    private readonly ConfluxContext _confluxContext;
    private readonly SCIMApiClient _scimApiClient;

    public ProjectSyncService(SCIMApiClient scimApiClient, ConfluxContext confluxContext)
    {
        _scimApiClient = scimApiClient;
        _confluxContext = confluxContext;
    }

    /// <summary>
    /// TODO: Retrieve all the groups for the collaboration, and check for updates in the members.
    /// </summary>
    /// <param name="projectId"></param>
    /// <exception cref="ProjectNotFoundException"></exception>
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

        // Sync the project info
        project.Title = apiProject.DisplayName;
        project.Description = apiProject.SCIMGroupInfo.Description;
    }
}
