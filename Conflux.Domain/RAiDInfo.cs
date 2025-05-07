// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain;

[Owned]
public record RAiDInfo
{
    public DateTime? LatestSync { get; set; }
    public bool Dirty { get; set; }
    [JsonPropertyName("raid_id")] public string? RAiDId { get; init; }
}
