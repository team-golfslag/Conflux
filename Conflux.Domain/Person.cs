using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a person.
/// </summary>
public class Person
{
    [Key] public Guid Id { get; set; }

    [Required] public required string Name { get; set; }
}
