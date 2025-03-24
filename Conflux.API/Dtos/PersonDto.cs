using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Conflux.Domain;

namespace Conflux.API.DTOs;

/// <summary>
/// The Data Transfer Object for <see cref="Project"/>
/// </summary>
public class PersonDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Converts a <see cref="PersonDto"/> to a <see cref="Person"/>
    /// </summary>
    /// <returns>The converted <see cref="Person"/></returns>
    public Person ToPerson() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = Name,
        };
}
