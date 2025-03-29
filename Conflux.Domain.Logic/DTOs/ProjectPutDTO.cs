namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for updating a <see cref="Project" /> with PUT.
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ProjectPutDTO
#pragma warning restore S101
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }
}
