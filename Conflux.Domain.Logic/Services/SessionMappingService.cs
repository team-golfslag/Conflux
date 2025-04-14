// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Models;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// This class maps all the data in the session to the existing domain model
/// </summary>
public class SessionMappingService
{
    private readonly ConfluxContext _context;
    private readonly IVariantFeatureManager _featureManager;
    private readonly SCIMApiClient _sramApiClient;

    /// <summary>
    /// Constructs a new <see cref="SessionMappingService" />.
    /// </summary>
    /// <param name="context">The Conflux context.</param>
    /// <param name="sramApiClient">The API client which is used to retrieve all user information.</param>
    public SessionMappingService(ConfluxContext context, SCIMApiClient sramApiClient,
        IVariantFeatureManager featureManager)
    {
        _featureManager = featureManager;
        _context = context;
        _sramApiClient = sramApiClient;
    }

    /// <summary>
    /// Collects all the data from the user session.
    /// </summary>
    /// <param name="userSession">The user session to collect the data from.</param>
    public async Task CollectSessionData(UserSession userSession)
    {
        if (!await _featureManager.IsEnabledAsync("SRAMAuthentication"))
            return;

        await CollectAndAddProjects(userSession);
        await _context.SaveChangesAsync();

        await CollectAndAddPeople(userSession);

        await CollectAndAddRoles(userSession);

        // Intermediate save is needed for coupling the projects, people and roles
        await _context.SaveChangesAsync();

        await CouplePeopleToProject(userSession);

        await CoupleRolesToPeople(userSession);

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Collects all projects from the user session and add them to the <see cref="ConfluxContext" />.
    /// </summary>
    /// <param name="userSession">The user session to collect the projects from.</param>
    private async Task CollectAndAddProjects(UserSession userSession)
    {
        foreach (Group group in userSession.Collaborations.Select(collaboration => collaboration.CollaborationGroup))
        {
            Project? existingCollaboration =
                await _context.Projects.SingleOrDefaultAsync(p => p.SRAMId == group.SRAMId);
            if (existingCollaboration is null)
            {
                // We only add the project, the members are added in the next step
                _context.Projects.Add(new()
                {
                    SRAMId = group.SRAMId,
                    Title = group.DisplayName,
                    Description = group.Description,
                    StartDate = DateTime.SpecifyKind(group.Created, DateTimeKind.Utc),
                });
            }
            else
            {
                existingCollaboration.Title = group.DisplayName;
                existingCollaboration.Description = group.Description;
            }
        }
    }

    private async Task CollectAndAddPeople(UserSession userSession)
    {
        foreach (Group group in userSession.Collaborations.Select(collaboration => collaboration.CollaborationGroup))
        {
            foreach (GroupMember member in group.Members)
            {
                SCIMUser? scimUser = await _sramApiClient.GetSCIMMemberByExternalId(member.SRAMId);
                if (scimUser is null) continue;
                Person retrievedPerson = await _context.People.SingleOrDefaultAsync(p => p.SRAMId == scimUser.Id) ??
                    new()
                    {
                        SRAMId = scimUser.Id,
                        Name = scimUser.DisplayName ?? scimUser.UserName ?? string.Empty,
                        GivenName = scimUser.Name?.GivenName,
                        FamilyName = scimUser.Name?.FamilyName,
                        Email = scimUser.Emails?.FirstOrDefault()?.Value,
                    };

                Person? existingPerson = await _context.People
                    .SingleOrDefaultAsync(p => p.SRAMId == retrievedPerson.SRAMId);
                if (existingPerson is not null) continue;
                _context.People.Add(retrievedPerson);
            }
        }
    }

    /// <summary>
    /// Collects the <see cref="Role" />s which are present in the Projects in the user session,
    /// and adds the to the <see cref="ConfluxContext" />.
    /// </summary>
    /// <param name="userSession">The user session to collect the roles from.</param>
    private async Task CollectAndAddRoles(UserSession userSession)
    {
        foreach (var collaboration in userSession.Collaborations)
        {
            Project? projects = await _context.Projects
                .SingleOrDefaultAsync(p => p.SRAMId == collaboration.CollaborationGroup.SRAMId);
            if (projects is null) continue;
            foreach (Role newRole in collaboration.Groups.Select(group => new Role
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projects.Id,
                    Name = group.DisplayName,
                    Description = group.Description,
                    Urn = group.Urn,
                }))
            {
                Role? existingRole = await _context.Roles
                    .SingleOrDefaultAsync(r => r.Urn == newRole.Urn);
                if (existingRole is not null) continue;
                _context.Roles.Add(newRole);
            }
        }
    }

    /// <summary>
    /// Couples people to projects based on the user session data.
    /// </summary>
    /// <param name="userSession">The user session containing the data to couple people to projects.</param>
    private async Task CouplePeopleToProject(UserSession userSession)
    {
        foreach (Group group in userSession.Collaborations.Select(collaboration => collaboration.CollaborationGroup))
        {
            Project? project = await _context.Projects
                .Include(p => p.People)
                .SingleOrDefaultAsync(p => p.SRAMId == group.SRAMId);

            var people = await _context.People
                .Where(p => group.Members.Select(m => m.SRAMId).Contains(p.SRAMId))
                .ToListAsync();

            foreach (Person person in people)
            {
                if (project is null) continue;
                if (project.People.Contains(person)) continue;

                project.People.Add(person);
            }
        }
    }

    /// <summary>
    /// Couples roles to people based on the user session data.
    /// </summary>
    /// <param name="userSession">The user session containing the data to couple roles to people.</param>
    private async Task CoupleRolesToPeople(UserSession userSession)
    {
        foreach (var groups in userSession.Collaborations.Select(collaboration => collaboration.Groups))
        {
            foreach (Group group in groups)
            {
                var people = await _context.People
                    .Where(p => group.Members
                        .Select(m => m.SRAMId)
                        .Contains(p.SRAMId))
                    .Include(person => person.Roles)
                    .ToListAsync();

                foreach (var roles in people.Select(role => role.Roles))
                {
                    Role? role = await _context.Roles
                        .SingleOrDefaultAsync(r => r.Urn == group.Urn);
                    if (role is null) continue;
                    if (roles.Contains(role)) continue;
                    roles.Add(role);
                }
            }
        }
    }
}
