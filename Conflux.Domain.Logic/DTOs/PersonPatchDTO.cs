namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for patching a <see cref="Person" />
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class PersonPatchDTO
#pragma warning restore S101
{
    public string? Name { get; init; }
}
