// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using SRAM.SCIM.Net;
using SRAM.SCIM.Net.Models;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// This class maps all the data in the session to the existing domain model
/// </summary>
public class SessionMappingService : ISessionMappingService
{
    private readonly ConfluxContext _context;
    private readonly IVariantFeatureManager _featureManager;
    private readonly ISCIMApiClient _sramApiClient;

    /// <summary>
    /// Constructs a new <see cref="SessionMappingService" />.
    /// </summary>
    /// <param name="context">The Conflux context.</param>
    /// <param name="sramApiClient">The API client which is used to retrieve all user information.</param>
    /// <param name="featureManager">The feature manager.</param>
    public SessionMappingService(ConfluxContext context, ISCIMApiClient sramApiClient,
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

        await CollectAndAddUsers(userSession);

        await CollectAndAddRoles(userSession);

        await CoupleUsersToProject(userSession);

        await CoupleRolesToUsers(userSession);

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
                await _context.Projects.SingleOrDefaultAsync(p => p.SCIMId == group.SCIMId);
            if (existingCollaboration is null)
            {
                Guid projectId = Guid.NewGuid();
                // We only add the project, the members are added in the next step
                _context.Projects.Add(new()
                {
                    Id = projectId,
                    SCIMId = group.SCIMId,
                    Titles =
                    [
                        new()
                        {
                            ProjectId = projectId,
                            Text = group.DisplayName,
                            Type = TitleType.Primary,
                            StartDate = DateTime.SpecifyKind(group.Created, DateTimeKind.Utc),
                            EndDate = null,
                        },
                    ],
                    Descriptions = group.Description == null
                        ? []
                        :
                        [
                            new()
                            {
                                ProjectId = projectId,
                                Text = group.Description,
                                Type = DescriptionType.Primary,
                            },
                        ],
                    StartDate = DateTime.SpecifyKind(group.Created, DateTimeKind.Utc),
                });
            }
            else
            {
                existingCollaboration.Titles =
                [
                    new()
                    {
                        ProjectId = existingCollaboration.Id,
                        Text = group.DisplayName,
                        Type = TitleType.Primary,
                        StartDate = existingCollaboration.StartDate,
                        EndDate = null,
                    },
                ];
                existingCollaboration.Descriptions = group.Description == null
                    ? []
                    :
                    [
                        new()
                        {
                            ProjectId = existingCollaboration.Id,
                            Text = group.Description,
                            Type = DescriptionType.Primary,
                            Language = null,
                        },
                    ];
            }
        }
    }

    private async Task CollectAndAddUsers(UserSession userSession)
    {
        foreach (Group group in userSession.Collaborations.Select(collaboration => collaboration.CollaborationGroup))
        {
            foreach (GroupMember member in group.Members)
                await ProcessGroupMember(member, userSession);
            await _context.SaveChangesAsync();
        }
    }

    private async Task ProcessGroupMember(GroupMember member, UserSession userSession)
    {
        SCIMUser? scimUser = await _sramApiClient.GetSCIMMemberByExternalId(member.SCIMId);
        if (scimUser is null)
            return;

        User? existingPerson = await _context.Users.SingleOrDefaultAsync(p => p.SCIMId == scimUser.Id);
        if (existingPerson is not null)
        {
            if (existingPerson.SRAMId != null || existingPerson.Email != userSession.Email) return;
            existingPerson.SRAMId = userSession.SRAMId;
            _context.Users.Update(existingPerson);
            return;
        }

        User newUser = new()
        {
            SCIMId = scimUser.Id,
            Name = scimUser.DisplayName ?? scimUser.UserName ?? string.Empty,
            GivenName = scimUser.Name?.GivenName,
            FamilyName = scimUser.Name?.FamilyName,
            Email = scimUser.Emails?.FirstOrDefault()?.Value,
        };

        if (newUser.Email == userSession.Email)
            newUser.SRAMId = userSession.SRAMId;

        _context.Users.Add(newUser);
    }

    /// <summary>
    /// Collects the <see cref="UserRole" />s which are present in the Projects in the user session,
    /// and adds the to the <see cref="ConfluxContext" />.
    /// </summary>
    /// <param name="userSession">The user session to collect the roles from.</param>
    private async Task CollectAndAddRoles(UserSession userSession)
    {
        foreach (Collaboration collaboration in userSession.Collaborations)
        {
            Project? projects = await _context.Projects
                .SingleOrDefaultAsync(p => p.SCIMId == collaboration.CollaborationGroup.SCIMId);
            if (projects is null) continue;
            foreach (UserRole newRole in collaboration.Groups.Select(group => new UserRole
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projects.Id,
                    Type = GroupUrnToUserRoleType(group.Urn),
                    SCIMId = group.SCIMId,
                    Urn = group.Urn,
                }))
            {
                UserRole? existingRole = await _context.UserRoles
                    .SingleOrDefaultAsync(r => r.Urn == newRole.Urn);
                if (existingRole is not null) continue;
                _context.UserRoles.Add(newRole);
            }

            await _context.SaveChangesAsync();
        }
    }

    public static UserRoleType GroupUrnToUserRoleType(string urn)
    {
        if (urn.EndsWith("conflux-admin"))
            return UserRoleType.Admin;
        if (urn.EndsWith("conflux-contributor"))
            return UserRoleType.Contributor;
        if (urn.EndsWith("conflux-user"))
            return UserRoleType.User;
        throw new ArgumentOutOfRangeException(nameof(urn),
            $"The group urn {urn} does not match any known user role type.");
    }

    /// <summary>
    /// Couples users to projects based on the user session data.
    /// </summary>
    /// <param name="userSession">The user session containing the data to couple users to projects.</param>
    private async Task CoupleUsersToProject(UserSession userSession)
    {
        foreach (Group group in userSession.Collaborations.Select(collaboration => collaboration.CollaborationGroup))
        {
            Project? project = await _context.Projects
                .Include(p => p.Users)
                .SingleOrDefaultAsync(p => p.SCIMId == group.SCIMId);

            var users = await _context.Users
                .Where(p => group.Members.Select(m => m.SCIMId).Contains(p.SCIMId))
                .ToListAsync();

            foreach (User person in users)
            {
                if (project is null) continue;
                if (project.Users.Contains(person)) continue;

                project.Users.Add(person);
            }
        }
    }

    /// <summary>
    /// Couples roles to users based on the user session data.
    /// </summary>
    /// <param name="userSession">The user session containing the data to couple roles to users.</param>
    private async Task CoupleRolesToUsers(UserSession userSession)
    {
        foreach (var groups in userSession.Collaborations.Select(collaboration => collaboration.Groups))
        {
            foreach (Group group in groups)
            {
                var users = await _context.Users
                    .Where(p => group.Members
                        .Select(m => m.SCIMId)
                        .Contains(p.SCIMId))
                    .Include(person => person.Roles)
                    .ToListAsync();

                foreach (var roles in users.Select(role => role.Roles))
                {
                    UserRole? role = await _context.UserRoles
                        .SingleOrDefaultAsync(r => r.Urn == group.Urn);
                    if (role is null) continue;
                    if (roles.Contains(role)) continue;
                    roles.Add(role);
                }
            }
        }
    }
}
