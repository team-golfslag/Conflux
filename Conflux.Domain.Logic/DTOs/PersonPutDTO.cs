using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for updating a <see cref="Person" />
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class PersonPutDTO
#pragma warning restore S101
{
    [Required] public required string Name { get; init; }
}
