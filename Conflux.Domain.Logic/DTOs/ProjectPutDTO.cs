using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for updating a <see cref="Project" /> with PUT.
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ProjectPutDTO
#pragma warning restore S101
{
    [Required] public required string Title { get; init; }

    [Required] public required string Description { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }
}
