using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a party.
/// </summary>
public class Party
{
    [Key] public Guid Id { get; set; }

    [Required] [MaxLength(100)] public required string Name { get; set; }

    public override int GetHashCode() => Name.GetHashCode();
}
