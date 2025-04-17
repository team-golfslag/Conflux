// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMLink
{
    [JsonPropertyName("name")] public string? Name { get; init; }

    [JsonPropertyName("value")] public string? Value { get; init; }
}
