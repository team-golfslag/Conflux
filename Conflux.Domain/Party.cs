using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a party.
/// </summary>
public class Party
{
    [Key]
    public Guid Id { get; set; }

    public string Name { get; set; }
}
