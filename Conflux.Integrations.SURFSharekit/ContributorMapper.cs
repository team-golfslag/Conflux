// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using SURFSharekit.Net.Models.RepoItem;

namespace Conflux.Integrations.SURFSharekit;

public static class ContributorMapper
{
    public static Contributor? MapContributor(SURFSharekitAuthor author, Person person, Project project)
    {
        return new()
        {
            PersonId = person.Id,
            ProjectId = project.Id,
        };
    }
}
