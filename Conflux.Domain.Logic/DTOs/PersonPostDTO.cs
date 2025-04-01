using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for <see cref="Person" />
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class PersonPostDTO
#pragma warning restore S101
{
    [Required] public required string Name { get; init; }

    /// <summary>
    /// Converts a <see cref="PersonPostDTO" /> to a <see cref="Person" />
    /// </summary>
    /// <returns>The converted <see cref="Person" /></returns>
    public Person ToPerson() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = Name,
        };
}
