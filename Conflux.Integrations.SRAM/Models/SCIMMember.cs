// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMMember
{
    [JsonPropertyName("$ref")] public required string Ref { get; init; }

    [JsonPropertyName("display")] public required string Display { get; init; }

    [JsonPropertyName("value")] public required string Value { get; init; }
}
