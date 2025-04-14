using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMMeta
{
    [JsonPropertyName("created")] public DateTime Created { get; init; }

    [JsonPropertyName("lastModified")] public DateTime LastModified { get; init; }

    [JsonPropertyName("location")] public string Location { get; init; }

    [JsonPropertyName("resourceType")] public string ResourceType { get; init; }

    [JsonPropertyName("version")] public string Version { get; init; }
}
