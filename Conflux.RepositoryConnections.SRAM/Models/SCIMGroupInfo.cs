using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMGroupInfo
{
    [JsonPropertyName("description")] public string Description { get; set; }

    [JsonPropertyName("labels")] public List<string> Labels { get; set; }

    [JsonPropertyName("links")] public List<SCIMLink> Links { get; set; }

    [JsonPropertyName("urn")] public string Urn { get; set; }
}
