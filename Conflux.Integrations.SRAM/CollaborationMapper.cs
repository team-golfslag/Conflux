// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Models;
using Conflux.RepositoryConnections.SRAM.DTOs;
using Conflux.RepositoryConnections.SRAM.Exceptions;
using Conflux.RepositoryConnections.SRAM.Models;

namespace Conflux.RepositoryConnections.SRAM;

public class CollaborationMapper : ICollaborationMapper
{
    private readonly ConfluxContext _context;
    private readonly ISCIMApiClient _scimApiClient;

    public CollaborationMapper(ConfluxContext context, ISCIMApiClient scimApiClient)
    {
        _context = context;
        _scimApiClient = scimApiClient;
    }

    /// <summary>
    /// Maps a list of CollaborationDTOs to domain Collaboration objects.
    /// </summary>
    /// <param name="collaborationDTOs">The list of CollaborationDTOs to map</param>
    /// <returns>A list of domain Collaboration objects</returns>
    public async Task<List<Collaboration>> Map(List<CollaborationDTO> collaborationDTOs)
    {
        var urns = new List<string>();
        foreach (CollaborationDTO collaborationDto in collaborationDTOs)
        {
            string groupUrn = FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name);
            urns.Add(groupUrn);
            urns.AddRange(collaborationDto.Groups.Select(g =>
                FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name, g)));
        }

        // Check if all urns have a matching ID in the database if this is not the case we are better off
        //  just retrieving all groups from the SCIM API
        if (urns.Any(urn => _context.SRAMGroupIdConnections.Find(urn) == null))
            return await GetAllGroupsFromSCIMApi(collaborationDTOs);

        var collaborations = new List<Collaboration>();
        foreach (CollaborationDTO collaborationDto in collaborationDTOs)
        {
            Collaboration collaboration = await GetCollaborationFromSCIMApi(collaborationDto);
            collaborations.Add(collaboration);
        }

        return collaborations;
    }

    /// <summary>
    /// Retrieves all groups from the SCIM API and maps them to collaborations.
    /// This method is called when not all URNs are found in the database cache.
    /// </summary>
    /// <param name="collaborationDtos">The list of CollaborationDTOs to map</param>
    /// <returns>A list of domain Collaboration objects</returns>
    /// <exception cref="GroupNotFoundException">Thrown when no groups are found in the SCIM API</exception>
    public async Task<List<Collaboration>> GetAllGroupsFromSCIMApi(List<CollaborationDTO> collaborationDtos)
    {
        var allGroups = await _scimApiClient.GetAllGroups();
        if (allGroups == null)
            throw new GroupNotFoundException("No groups found in SCIM API");

        Dictionary<string, SCIMGroup> groupMap = new();
        List<SRAMGroupIdConnection> allConnections = [];
        foreach (SCIMGroup group in allGroups)
        {
            string groupUrn = "urn:mace:surf.nl:sram:group:" + group.SCIMGroupInfo.Urn;
            string id = group.Id;
            allConnections.Add(new()
            {
                Urn = groupUrn,
                Id = id,
            });
            groupMap[groupUrn] = group;
        }

        // Add all connections to the database by first removing all existing connections 
        await _context.SaveChangesAsync();
        _context.SRAMGroupIdConnections.RemoveRange(_context.SRAMGroupIdConnections);
        _context.SRAMGroupIdConnections.AddRange(allConnections);
        await _context.SaveChangesAsync();

        // Map the groups to collaborations
        var collaborations = new List<Collaboration>();
        foreach (CollaborationDTO collaborationDto in collaborationDtos)
        {
            string collaborationGroupUrn = FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name);
            Group collaborationGroup = MapSCIMGroup(collaborationGroupUrn, groupMap[collaborationGroupUrn]);

            List<Group> groups = [];
            groups.AddRange(collaborationDto.Groups
                .Select(groupId =>
                    FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name, groupId))
                .Select(groupUrn =>
                    MapSCIMGroup(groupUrn, groupMap[groupUrn])));

            Collaboration collaboration = new()
            {
                Organization = collaborationDto.Organization,
                CollaborationGroup = collaborationGroup,
                Groups = groups,
            };

            collaborations.Add(collaboration);
        }

        return collaborations;
    }

    /// <summary>
    /// Maps a single CollaborationDTO to a domain Collaboration object by retrieving
    /// group information from the SCIM API.
    /// </summary>
    /// <param name="collaborationDto">The CollaborationDTO to map</param>
    /// <returns>A domain Collaboration object</returns>
    public async Task<Collaboration> GetCollaborationFromSCIMApi(CollaborationDTO collaborationDto)
    {
        string collaborationGroupUrn = FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name);
        Group collaborationGroup = await GetGroupFromSCIMApi(collaborationGroupUrn);

        List<Group> groups = [];
        foreach (string groupUrn in collaborationDto.Groups.Select(groupId =>
            FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name, groupId)))
        {
            Group group = await GetGroupFromSCIMApi(groupUrn);
            groups.Add(group);
        }

        Collaboration collaboration = new()
        {
            Organization = collaborationDto.Organization,
            CollaborationGroup = collaborationGroup,
            Groups = groups,
        };

        return collaboration;
    }

    /// <summary>
    /// Formats a group URN based on organization name, collaboration name, and optional group name.
    /// </summary>
    /// <param name="orgName">The organization name</param>
    /// <param name="coName">The collaboration name</param>
    /// <param name="groupName">Optional group name</param>
    /// <returns>Formatted URN string</returns>
    public static string FormatGroupUrn(string orgName, string coName, string? groupName = null)
        // see https://servicedesk.surf.nl/wiki/spaces/IAM/pages/74226142/Attributes+in+SRAM
        =>
            string.IsNullOrEmpty(groupName)
                ? $"urn:mace:surf.nl:sram:group:{orgName}:{coName}"
                : $"urn:mace:surf.nl:sram:group:{orgName}:{coName}:{groupName}";

    /// <summary>
    /// Retrieves a group from the SCIM API based on its URN.
    /// </summary>
    /// <param name="groupUrn">The URN of the group to retrieve</param>
    /// <returns>A domain Group object</returns>
    /// <exception cref="GroupNotFoundException">Thrown when the group cannot be found</exception>
    public async Task<Group> GetGroupFromSCIMApi(string groupUrn)
    {
        SRAMGroupIdConnection? connection = await _context.SRAMGroupIdConnections
            .FindAsync(groupUrn);

        // There should always be a group with the given URN in the database
        if (connection == null)
            throw new GroupNotFoundException($"Group with URN {groupUrn} not found");

        // Get the group from the SCIM API
        SCIMGroup? scimGroup = await _scimApiClient.GetSCIMGroup(connection.Id);
        if (scimGroup == null)
            throw new GroupNotFoundException($"Group with ID {connection.Id} not found in SCIM API");

        // Map the SCIM group to a Group object
        return MapSCIMGroup(groupUrn, scimGroup);
    }

    /// <summary>
    /// Maps a SCIM group to a domain Group object.
    /// </summary>
    /// <param name="groupUrn">The URN of the group</param>
    /// <param name="scimGroup">The SCIM group to map</param>
    /// <returns>A domain Group object</returns>
    public static Group MapSCIMGroup(string groupUrn, SCIMGroup scimGroup)
    {
        // Get the members of the group
        List<GroupMember> members = [];
        members.AddRange(scimGroup.Members.Select(member => new GroupMember
        {
            DisplayName = member.Display,
            SCIMId = member.Value,
        }));

        string? url = scimGroup.SCIMGroupInfo.Links?.FirstOrDefault(l => l.Name == "sbs_url")?.Value;
        string? logoUrl = scimGroup.SCIMGroupInfo.Links?.FirstOrDefault(l => l.Name == "logo")?.Value;

        // Map the SCIM group to a Group object
        return new()
        {
            Id = scimGroup.SCIMGroupInfo.Urn,
            Urn = groupUrn,
            DisplayName = scimGroup.DisplayName,
            Description = scimGroup.SCIMGroupInfo.Description,
            Url = url,
            LogoUrl = logoUrl,
            ExternalId = scimGroup.ExternalId,
            SCIMId = scimGroup.Id,
            Members = members,
            Created = scimGroup.SCIMMeta.Created,
        };
    }
}
