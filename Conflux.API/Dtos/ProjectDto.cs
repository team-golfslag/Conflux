using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Conflux.Domain;

namespace Conflux.API.DTOs;

/// <summary>
/// The Data Transfer Object for <see cref="Project"/>
/// </summary>
public class ProjectDto
{
    [JsonPropertyName("id")] public Guid? Id { get; set; }

    [Required]
    [MaxLength(100)]
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("start_date")] public DateOnly? StartDate { get; set; }

    [JsonPropertyName("end_date")] public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Converts a <see cref="ProjectDto"/> to a <see cref="Project"/>
    /// </summary>
    /// <returns>The converted <see cref="Project"/></returns>
    public Project ToProject() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = Title,
            Description = Description,
            StartDate = StartDate,
            EndDate = EndDate,
        };
}
