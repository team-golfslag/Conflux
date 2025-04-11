// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Core.Models;
using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Models;
using Conflux.RepositoryConnections.SRAM.DTOs;
using Conflux.RepositoryConnections.SRAM.Models;

namespace Conflux.RepositoryConnections.SRAM;

public class CollaborationMapper(ConfluxContext context, SCIMApiClient scimApiClient)
{
    /// <summary>
    /// Maps a list of CollaborationDTOs to domain Collaboration objects.
    /// </summary>
    /// <param name="collaborationDtos">The list of CollaborationDTOs to map</param>
    /// <returns>A list of domain Collaboration objects</returns>
    public async Task<List<Collaboration>> Map(List<CollaborationDTO> collaborationDtos)
    {
        var urns = new List<string>();
        foreach (CollaborationDTO collaborationDto in collaborationDtos)
        {
            string groupUrn = FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name);
            urns.Add(groupUrn);
            urns.AddRange(collaborationDto.Groups.Select(g => FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name, g)));
        }
        
        // Check if all urns have a matching ID in the database if this is not the case we are better off
        //  just retrieving all groups from the SCIM API
        if (urns.Any(urn => !context.SRAMGroupIdConnections.Any(x => x.Urn == urn)))
            return await GetAllGroupsFromSCIMApi(collaborationDtos);
        
        var collaborations = new List<Collaboration>();
        foreach (CollaborationDTO collaborationDto in collaborationDtos)
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
    private async Task<List<Collaboration>> GetAllGroupsFromSCIMApi(List<CollaborationDTO> collaborationDtos)
    {
        var allGroups = await scimApiClient.GetAllGroups();
        if (allGroups == null)
            throw new("No groups found in SCIM API");

        Dictionary<string, SCIMGroup> groupMap = new();
        List<SRAMGroupIdConnection> allConnections = [];
        foreach (SCIMGroup group in allGroups)
        {
            string groupUrn = "urn:mace:surf.nl:sram:group:" + group.SCIMGroupInfo.Urn;
            string id = group.Id;
            allConnections.Add(new()
            {
                Urn = groupUrn,
                Id = id
            });
            groupMap[groupUrn] = group;
        }
        
        // Add all connections to the database by first removing all existing connections 
        context.SRAMGroupIdConnections.RemoveRange(context.SRAMGroupIdConnections);
        context.SRAMGroupIdConnections.AddRange(allConnections);
        await context.SaveChangesAsync();
        
        // Map the groups to collaborations
        // TODO: do we need checking here to ensure all urns are present in the groupMap?
        var collaborations = new List<Collaboration>();
        foreach (CollaborationDTO collaborationDto in collaborationDtos)
        {
            string collaborationGroupUrn = FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name);
            Group collaborationGroup = MapSCIMGroup(collaborationGroupUrn, groupMap[collaborationGroupUrn]);
            
            List<Group> groups = [];
            foreach (string groupId in collaborationDto.Groups)
            {
                string groupUrn = FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name, groupId);
                Group group = MapSCIMGroup(groupUrn, groupMap[groupUrn]);
                groups.Add(group);
            }
            
            Collaboration collaboration = new()
            {
                Organization = collaborationDto.Organization,
                CollaborationGroup = collaborationGroup,
                Groups = groups
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
    private async Task<Collaboration> GetCollaborationFromSCIMApi(CollaborationDTO collaborationDto)
    {
        string collaborationGroupUrn = FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name);
        Group collaborationGroup = await GetGroupFromSCIMApi(collaborationGroupUrn);
        
        List<Group> groups = new();
        foreach (string groupId in collaborationDto.Groups)
        {
            string groupUrn = FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name, groupId);
            Group group = await GetGroupFromSCIMApi(groupUrn);
            groups.Add(group);
        }
        
        Collaboration collaboration = new()
        {
            Organization = collaborationDto.Organization,
            CollaborationGroup = collaborationGroup,
            Groups = groups
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
    private string FormatGroupUrn(string orgName, string coName, string? groupName = null)
    {
        // see https://servicedesk.surf.nl/wiki/spaces/IAM/pages/74226142/Attributes+in+SRAM
        if (string.IsNullOrEmpty(groupName))
            return $"urn:mace:surf.nl:sram:group:{orgName}:{coName}";
        return $"urn:mace:surf.nl:sram:group:{orgName}:{coName}:{groupName}";
    }

    /// <summary>
    /// Retrieves a group from the SCIM API based on its URN.
    /// </summary>
    /// <param name="groupUrn">The URN of the group to retrieve</param>
    /// <returns>A domain Group object</returns>
    /// <exception cref="Exception">Thrown when the group cannot be found</exception>
    private async Task<Group> GetGroupFromSCIMApi(string groupUrn)
    {
        var connection = await context.SRAMGroupIdConnections
            .FindAsync(groupUrn);

        // There should always be a group with the given URN in the database
        if (connection == null)
            throw new($"Group with URN {groupUrn} not found in database");

        // Get the group from the SCIM API
        SCIMGroup? scimGroup = await scimApiClient.GetSCIMGroup(connection.Id);
        if (scimGroup == null)
            throw new($"Group with ID {connection.Id} not found in SCIM API");
        
        // Map the SCIM group to a Group object
        return MapSCIMGroup(groupUrn, scimGroup);
    }
    
    /// <summary>
    /// Maps a SCIM group to a domain Group object.
    /// </summary>
    /// <param name="groupUrn">The URN of the group</param>
    /// <param name="scimGroup">The SCIM group to map</param>
    /// <returns>A domain Group object</returns>
    private Group MapSCIMGroup(string groupUrn, SCIMGroup scimGroup)
    {
        // Get the members of the group
        List<GroupMember> members = [];
        foreach (SCIMMember member in scimGroup.Members)
        {
            GroupMember groupMember = new()
            {
                DisplayName = member.Display,
                SRAMId = member.Value
            };
            members.Add(groupMember);
        }
        
        string? url = scimGroup.SCIMGroupInfo.Links?.FirstOrDefault(l => l.Name == "sbs_url")?.Value;
        string? logoUrl = scimGroup.SCIMGroupInfo.Links?.FirstOrDefault(l => l.Name == "logo")?.Value;
        
        // Map the SCIM group to a Group object
        return new Group()
        {
            Id = scimGroup.SCIMGroupInfo.Urn,
            Urn = groupUrn,
            DisplayName = scimGroup.DisplayName,
            Description = scimGroup.SCIMGroupInfo.Description,
            Url = url,
            LogoUrl = logoUrl,
            ExternalId = scimGroup.ExternalId,
            SRAMId = scimGroup.Id,
            Members = members,
        };
    }
}
