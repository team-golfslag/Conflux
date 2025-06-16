// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.Integrations.Archive.Models;

/// <summary>
/// Represents the "counters" object within the status response.
/// </summary>
public record Spn2Counters
{
    [JsonPropertyName("embeds")]
    public int Embeds { get; init; }

    [JsonPropertyName("outlinks")]
    public int Outlinks { get; init; }
}