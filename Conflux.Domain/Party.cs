using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a party.
/// </summary>
public class Party
{
    [Key] public Guid Id { get; set; }

    [Required] public required string Name { get; set; }
}
