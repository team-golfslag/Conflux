// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Conflux.Domain;

/// <summary>
/// Represents a project.
/// </summary>
public class Project
{
    [Key] public Guid Id { get; init; }

    [JsonPropertyName("scim_id")]
    public string? SCIMId { get; init; }

    [JsonPropertyName("raid_id")]
    public string? RAiDId { get; init; }

    [Required] public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public List<User> Users { get; set; } = [];

    public List<Product> Products { get; set; } = [];

    public List<Party> Parties { get; set; } = [];
}
