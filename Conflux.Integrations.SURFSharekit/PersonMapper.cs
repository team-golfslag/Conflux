// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using SURFSharekit.Net.Models.RepoItem;

namespace Conflux.Integrations.SURFSharekit;

public static class PersonMapper
{
    public static Person? MapPerson(SURFSharekitPerson person)
    {
        // Person has a required name
        if (string.IsNullOrWhiteSpace(person.Name))
        {
            return null;
        }

        return new()
        {
            Name = person.Name,
            Email = person.Email,
            ORCiD = person.Orcid,
        };
    }
}
