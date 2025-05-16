// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using SURFSharekit.Net.Models.RepoItem;

namespace Conflux.Integrations.SURFSharekit;

public static class OrganisationMapper
{
    public static Organisation? MapOrganisation(SURFSharekitOwner owner)
    {
        // Organisation has a required name
        return string.IsNullOrWhiteSpace(owner.Name) ? null : new()
        {
            Name = owner.Name,
        };
    }
}
