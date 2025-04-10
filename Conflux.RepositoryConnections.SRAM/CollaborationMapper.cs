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
    public async Task<List<Collaboration>> Map(List<CollaborationDTO> collaborationDtos)
    {
        var urns = new List<string>();
        foreach (CollaborationDTO collaborationDto in collaborationDtos)
        {
            string groupUrn = FormatGroupUrn(collaborationDto.Organization, collaborationDto.Name);
            urns.Add(groupUrn);
            urns.AddRange(collaborationDto.Groups.Select(g => FormatGroupUrn(g, collaborationDto.Name, groupUrn)));
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
    
    // TODO: find better name
    // TODO: i think this is always even when the urns are present in the database
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
    
  
    private string FormatGroupUrn(string orgName, string coName, string? groupName = null)
    {
        // see https://servicedesk.surf.nl/wiki/spaces/IAM/pages/74226142/Attributes+in+SRAM
        if (string.IsNullOrEmpty(groupName))
            return $"urn:mace:surf.nl:sram:group:{orgName}:{coName}";
        return $"urn:mace:surf.nl:sram:group:{orgName}:{coName}:{groupName}";
    }

    private async Task<Group> GetGroupFromSCIMApi(string groupUrn)
    {
        string? id = context.SRAMGroupIdConnections
            .FirstOrDefault(x => x.Urn == groupUrn)?.Id;

        // There should always be a group with the given URN in the database
        if (id == null)
            throw new($"Group with URN {groupUrn} not found in database");

        // Get the group from the SCIM API
        SCIMGroup? scimGroup = await scimApiClient.GetSCIMGroup(id);
        if (scimGroup == null)
            throw new($"Group with ID {id} not found in SCIM API");
        
        // Map the SCIM group to a Group object
        return MapSCIMGroup(groupUrn, scimGroup);
    }
    
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
