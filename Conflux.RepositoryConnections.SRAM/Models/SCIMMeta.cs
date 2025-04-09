using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMMeta
{
    [JsonPropertyName("created")] public DateTime Created { get; set; }

    [JsonPropertyName("lastModified")] public DateTime LastModified { get; set; }

    [JsonPropertyName("location")] public string Location { get; set; }

    [JsonPropertyName("resourceType")] public string ResourceType { get; set; }

    [JsonPropertyName("version")] public string Version { get; set; }
}
