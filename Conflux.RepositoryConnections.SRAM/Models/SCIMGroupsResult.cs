using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMGroupsResult
{
    [JsonPropertyName("Resources")] public List<SCIMGroup> Groups { get; set; }

    [JsonPropertyName("itemsPerPage")] public int ItemsPerPage { get; set; }

    [JsonPropertyName("schemas")] public List<string> Schemas { get; set; }

    [JsonPropertyName("startIndex")] public int StartIndex { get; set; }

    [JsonPropertyName("totalResults")] public int TotalResults { get; set; }
}
