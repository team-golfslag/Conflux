// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMMeta
{
    [JsonPropertyName("created")] public DateTime Created { get; init; }

    [JsonPropertyName("lastModified")] public DateTime LastModified { get; init; }

    [JsonPropertyName("location")] public required string Location { get; init; }

    [JsonPropertyName("resourceType")] public required string ResourceType { get; init; }

    [JsonPropertyName("version")] public required string Version { get; init; }
}
