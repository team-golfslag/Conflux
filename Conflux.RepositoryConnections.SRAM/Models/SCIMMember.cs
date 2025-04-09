using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMMember
{
    [JsonPropertyName("$ref")] public string Ref { get; set; }

    [JsonPropertyName("display")] public string Display { get; set; }

    [JsonPropertyName("value")] public string Value { get; set; }
}
