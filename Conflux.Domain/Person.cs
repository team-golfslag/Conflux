// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Conflux.Domain;

/// <summary>
/// Represents a person.
/// </summary>
public record Person
{
    [Key] public Guid Id { get; set; }

    public string? SRAMId { get; set; }
    [JsonPropertyName("orcid_id")]
    public string? ORCiD { get; set; }

    [Required] public required string Name { get; set; }

    public List<Role> Roles { get; set; } = [];

    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Email { get; set; }
}
