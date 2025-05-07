// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a party.
/// </summary>
public class Organisation
{
    [Key] public Guid Id { get; init; }

    public string SchemaUri => "https://ror.org/";

    public string? RORId { get; set; }

    public List<OrganisationRole> Roles { get; set; } = [];

    [Required] public required string Name { get; set; }
}
