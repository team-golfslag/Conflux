// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Conflux.Domain;

public class UserRole
{
    [Key] public required Guid Id { get; init; }
    [ForeignKey(nameof(Project))] public required Guid ProjectId { get; init; }

    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Urn { get; init; }

    [JsonPropertyName("scim_id")] public required string SCIMId { get; init; }
}
