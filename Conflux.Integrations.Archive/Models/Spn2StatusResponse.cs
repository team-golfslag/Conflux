// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.Integrations.Archive.Models;

/// <summary>
/// Represents the complete JSON response from the SPN2 /save/status/{job_id}
/// endpoint, reflecting the modern, detailed structure.
/// </summary>
public record Spn2StatusResponse
{
    [JsonPropertyName("counters")]
    public Spn2Counters? Counters { get; init; }

    [JsonPropertyName("duration_sec")]
    public double DurationSec { get; init; }

    [JsonPropertyName("http_status")]
    public int HttpStatus { get; init; }

    [JsonPropertyName("job_id")]
    public string? JobId { get; init; }

    [JsonPropertyName("original_url")]
    public string? OriginalUrl { get; init; }

    [JsonPropertyName("outlinks")]
    public List<string>? Outlinks { get; init; }

    [JsonPropertyName("resources")]
    public List<string>? Resources { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    [JsonPropertyName("exception")]
    public string? Exception { get; init; }
}
