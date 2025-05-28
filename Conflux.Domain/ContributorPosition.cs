// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain;

/// <summary>
/// TODO: Move to RAiD Package.
/// </summary>
public enum ContributorPositionType
{
    PrincipalInvestigator = 307,
    CoInvestigator = 308,
    Partner = 309,
    Consultant = 310,
    Other = 311,
}

[PrimaryKey(nameof(PersonId), nameof(ProjectId), nameof(Position))]
public class ContributorPosition
{
    public required Guid PersonId { get; init; }
    
    public required Guid ProjectId { get; init; }
    
    public Contributor? Contributor { get; init; }

    public required ContributorPositionType Position { get; init; }
    public string SchemaUri => "https://vocabulary.raid.org/contributor.position.schema/305";
    public string GetUri => "https://vocabulary.raid.org/contributor.position.schema/" + (int)Position;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
