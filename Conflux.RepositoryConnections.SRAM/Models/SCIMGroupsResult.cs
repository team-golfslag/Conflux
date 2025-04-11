using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMGroupsResult
{
    [JsonPropertyName("Resources")] public List<SCIMGroup> Groups { get; init; }

    [JsonPropertyName("itemsPerPage")] public int ItemsPerPage { get; init; }

    [JsonPropertyName("schemas")] public List<string> Schemas { get; init; }

    [JsonPropertyName("startIndex")] public int StartIndex { get; init; }

    [JsonPropertyName("totalResults")] public int TotalResults { get; init; }
}
