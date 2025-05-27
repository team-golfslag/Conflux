// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain;

[PrimaryKey(nameof(PersonId), nameof(ProjectId))]
public record Contributor
{
    public Guid PersonId { get; init; }
    public Person? Person { get; init; }

    public required Guid ProjectId { get; init; }
    public Project? Project { get; init; }

    public List<ContributorRole> Roles { get; set; } = [];
    public List<ContributorPosition> Positions { get; set; } = [];

    /// <summary>
    /// True if this contributor is a leader. Multiple leaders are allowed but 1 is required in RAiD
    /// </summary>
    public bool Leader { get; set; }

    /// <summary>
    /// True if this contributor is a contact. Multiple contacts are allowed but 1 is required in RAiD
    /// </summary>
    public bool Contact { get; set; }
}
