// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using System.Text.RegularExpressions;
using Conflux.RepositoryConnections.SRAM.DTOs;

namespace Conflux.RepositoryConnections.SRAM.Extensions;

public static partial class ClaimsPrincipleExtensions
{
    public static string? GetClaimValue(this ClaimsPrincipal claimsPrincipal, string claimType)
    {
        return claimsPrincipal.Claims
            .FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    public static List<CollaborationDTO> GetCollaborations(this ClaimsPrincipal claimsPrincipal)
    {
        Regex regex = EntitlementRegex();

        var matches = claimsPrincipal.Claims
            .Where(c => c.Type == "Role")
            .Select(c => regex.Match(c.Value))
            .Where(m => m.Success);

        Dictionary<(string, string), List<string>> collaborationGroups = [];
        foreach (GroupCollection groups in matches.Select(m => m.Groups))
        {
            string organisation = groups[1].Value;
            string collaborationName = groups[2].Value;
            string? groupId = groups[3].Success ? groups[3].Value : null;

            if (!collaborationGroups.ContainsKey((organisation, collaborationName)))
                collaborationGroups[(organisation, collaborationName)] = [];
            if (groupId == null) continue;

            collaborationGroups[(organisation, collaborationName)].Add(groupId);
        }

        List<CollaborationDTO> collaborations = [];
        foreach (((string, string) key, var groupIds) in collaborationGroups)
        {
            (string organisation, string collaborationName) = key;
            collaborations.Add(new()
            {
                Name = collaborationName,
                Organization = organisation,
                Groups = groupIds,
            });
        }

        return collaborations;
    }

    [GeneratedRegex(@"^urn:mace:surf\.nl:sram:group:([a-z1-9_]+):([a-z1-9_]+)(?::(conflux-[a-z1-9_]+))?$")]
    private static partial Regex EntitlementRegex();
}
