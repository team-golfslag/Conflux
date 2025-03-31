namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for updating a <see cref="Project" /> with PATCH.
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ProjectPatchDTO
#pragma warning restore S101
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }
}
