// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain;

[PrimaryKey(nameof(ProjectId), nameof(OrganisationId))]
public class ProjectOrganisation
{
    [Key] [Column(Order = 0)] public Guid ProjectId { get; init; }
    public Project? Project { get; init; }

    [Key] [Column(Order = 1)] public Guid OrganisationId { get; init; }
    public Organisation? Organisation { get; init; }

    public List<OrganisationRole> Roles { get; set; } = [];
}
