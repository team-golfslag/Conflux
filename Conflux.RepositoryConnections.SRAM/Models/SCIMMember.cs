using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMMember
{
    [JsonPropertyName("$ref")] public required string Ref { get; init; }

    [JsonPropertyName("display")] public required string Display { get; init; }

    [JsonPropertyName("value")] public required string Value { get; init; }
}
