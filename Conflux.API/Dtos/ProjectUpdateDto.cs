using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Conflux.Domain;

namespace Conflux.API.DTOs;

/// <summary>
/// The Data Transfer Object for updating a <see cref="Project"/>
/// </summary>
public class ProjectUpdateDto
{
    [MaxLength(100)]
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("start_date")]
    public DateOnly? StartDate { get; set; }
    
    [JsonPropertyName("end_date")]
    public DateOnly? EndDate { get; set; }
}
