using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for <see cref="Person" />
/// </summary>
public class PersonDTO
{
    [Required] public required string Name { get; init; }

    /// <summary>
    /// Converts a <see cref="PersonDTO" /> to a <see cref="Person" />
    /// </summary>
    /// <returns>The converted <see cref="Person" /></returns>
    public Person ToPerson() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = Name,
        };
}
