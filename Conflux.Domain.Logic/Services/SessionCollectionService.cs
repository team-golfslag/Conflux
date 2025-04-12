// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Models;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.Models;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

public class SessionCollectionService
{
    private readonly ConfluxContext _context;
    private readonly SCIMApiClient _sramApiClient;

    public SessionCollectionService(ConfluxContext context, SCIMApiClient sramApiClient)
    {
        _sramApiClient = sramApiClient;
        _context = context;
    }

    public async Task HandleSession(UserSession userSession)
    {
        foreach (Collaboration collaboration in userSession.Collaborations)
        {
            Group group = collaboration.CollaborationGroup;
            Project? existingCollaboration =
                await _context.Projects.SingleOrDefaultAsync(p => p.SRAMId == group.SRAMId);
            if (existingCollaboration is null)
            {
                List<Person> people = [];
                foreach (GroupMember member in group.Members)
                {
                    SCIMUser? scimUser = await _sramApiClient.GetSCIMMemberByExternalId(member.SRAMId);
                    // Check if the member is already in the database
                    if (scimUser is null) continue;
                    // We make the scimUser a person
                    // If not, we create a new person
                    Person retrievedPerson = await SCIMUserToPerson(scimUser) ?? FromSCIMUser(scimUser);

                    // Check if the person is already in the database
                    Person? existingPerson = await _context.People
                        .SingleOrDefaultAsync(p => p.SRAMId == retrievedPerson.SRAMId);
                    if (existingPerson is null)
                    {
                        // If not, add it to the database
                        _context.People.Add(retrievedPerson);
                        people.Add(retrievedPerson);
                    }
                    else
                    {
                        // If it is, just add it to the list of people
                        people.Add(existingPerson);
                    }
                }

                _context.Projects.Add(new()
                {
                    SRAMId = group.SRAMId,
                    Title = group.DisplayName,
                    Description = group.Description,
                    People = people,
                });
            }
            else
            {
                existingCollaboration.Title = group.DisplayName;
                existingCollaboration.Description = group.Description;
                foreach (GroupMember member in group.Members)
                {
                    SCIMUser? scimUser = await _sramApiClient.GetSCIMMemberByExternalId(member.SRAMId);
                    // Check if the member is already in the database
                    if (scimUser is null) continue;
                    // We make the scimUser a person
                    // If not, we create a new person
                    Person retrievedPerson = await SCIMUserToPerson(scimUser) ?? FromSCIMUser(scimUser);

                    // Check if the person is already in the database
                    Person? existingPerson = await _context.People
                        .SingleOrDefaultAsync(p => p.SRAMId == retrievedPerson.SRAMId);
                    if (existingPerson is not null) continue;
                    // If not, add it to the database
                    _context.People.Add(retrievedPerson);
                    existingCollaboration.People.Add(retrievedPerson);
                }
            }

            await _context.SaveChangesAsync();

            // TODO: Make this working concurrently
            continue;
            var subgroupMemberIds = collaboration.Groups
                .SelectMany(subGroup => subGroup.Members.Select(member => member.SRAMId))
                .Distinct()
                .ToList();

            var personsDictionary = await _context.People
                .Include(p => p.Roles)
                .Where(p => subgroupMemberIds.Contains(p.SRAMId))
                .ToDictionaryAsync(p => p.SRAMId);

            // Now iterate over subgroups using these pre-loaded instances.
            foreach (Group subGroup in collaboration.Groups)
            {
                foreach (GroupMember member in subGroup.Members)
                {
                    if (!personsDictionary.TryGetValue(member.SRAMId, out Person existingPerson))
                        // Optionally log that no Person was found.
                        continue;

                    // Create the role based on the subgroup.
                    Role newRole = new()
                    {
                        Id = Guid.NewGuid(),
                        Name = subGroup.DisplayName,
                        Description = subGroup.Description,
                        Urn = subGroup.Urn,
                    };

                    // Check if a role with the same Urn is already associated.
                    if (existingPerson.Roles.All(r => r.Urn != newRole.Urn)) existingPerson.Roles.Add(newRole);
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task<Person?> SCIMUserToPerson(SCIMUser user) =>
        await _context.People.SingleOrDefaultAsync(p => p.SRAMId == user.Id);

    private static Person FromSCIMUser(SCIMUser scimUser) =>
        new()
        {
            SRAMId = scimUser.Id,
            Name = scimUser.DisplayName ?? scimUser.UserName ?? string.Empty,
            GivenName = scimUser.Name?.GivenName,
            FamilyName = scimUser.Name?.FamilyName,
            Email = scimUser.Emails?.FirstOrDefault()?.Value,
        };
}
