// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMGroupsResult
{
    [JsonPropertyName("Resources")] public required List<SCIMGroup> Groups { get; init; }

    [JsonPropertyName("itemsPerPage")] public int ItemsPerPage { get; init; }

    [JsonPropertyName("schemas")] public required List<string> Schemas { get; init; }

    [JsonPropertyName("startIndex")] public int StartIndex { get; init; }

    [JsonPropertyName("totalResults")] public int TotalResults { get; init; }
}
