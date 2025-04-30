// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain;

[PrimaryKey(nameof(PersonId), nameof(ProjectId))]
public record Contributor
{
    [ForeignKey(nameof(Person))] public Guid PersonId { get; init; }
    [ForeignKey(nameof(Project))] public Guid ProjectId { get; init; }
    public List<ContributorRole> Roles { get; set; } = [];
    public List<ContributorPosition> Positions { get; set; } = [];

    public bool Leader { get; set; }
    public bool Contact { get; set; }
}
