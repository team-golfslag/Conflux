using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Conflux.Domain;

namespace Conflux.Core.DTOs;

public class ProjectDTO
{
    [JsonPropertyName("id")] public Guid? Id { get; set; }

    [Required]
    [MaxLength(100)]
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("start_date")] public DateOnly? StartDate { get; set; }

    [JsonPropertyName("end_date")] public DateOnly? EndDate { get; set; }

    public Project ToProject()
    {
        DateTime startDate = StartDate.ToDateTime(TimeOnly.MinValue);
        DateTime endDate = EndDate.ToDateTime(TimeOnly.MinValue);
        return new()
        {
            Id = Guid.NewGuid(),
            Title = Title,
            Description = Description,
            StartDate = startDate,
            EndDate = endDate,
        };
    }
}
