using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for <see cref="Project" /> with POST.
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ProjectPostDTO
#pragma warning restore S101
{
    public Guid? Id { get; init; }

    [Required] public required string Title { get; init; }

    public string? Description { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Converts a <see cref="ProjectPostDTO" /> to a <see cref="Project" />
    /// </summary>
    /// <returns>The converted <see cref="Project" /></returns>
    public Project ToProject() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = Title,
            Description = Description,
            StartDate = StartDate.HasValue ? DateTime.SpecifyKind(StartDate.Value, DateTimeKind.Utc) : null,
            EndDate = EndDate.HasValue ? DateTime.SpecifyKind(EndDate.Value, DateTimeKind.Utc) : null,
        };
}
