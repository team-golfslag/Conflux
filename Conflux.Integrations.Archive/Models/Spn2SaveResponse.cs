// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.Integrations.Archive.Models;

public class Spn2SaveResponse
{
    [JsonPropertyName("job_id")]
    public string? JobId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("original_url")]
    public string? OriginalUrl { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("exception")]
    public string? Exception { get; set; }
}
