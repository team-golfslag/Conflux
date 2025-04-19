// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMGroupInfo
{
    [JsonPropertyName("description")] public string? Description { get; init; }

    [JsonPropertyName("labels")] public List<string>? Labels { get; init; }

    [JsonPropertyName("links")] public List<SCIMLink>? Links { get; init; }

    [JsonPropertyName("urn")] public required string Urn { get; init; }
}
