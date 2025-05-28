// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Conflux.Domain;

public record RAiDInfo
{
    [Key] public Guid ProjectId { get; init; }
    public Project? Project { get; init; }

    public DateTime? LatestSync { get; set; }
    public bool Dirty { get; set; }

    [JsonPropertyName("raid_id")] public string RAiDId { get; init; }
    public string SchemaUri => "https://raid.org/";


    public required string RegistrationAgencyId { get; init; }
    public string RegistrationAgencySchemaUri => "https://ror.org/";

    public required string OwnerId { get; init; }
    public string OwnerSchemaUri => "https://ror.org/";
    public long? OwnerServicePoint { get; init; }

    public string License => "Creative Commons CC-0";
    public int Version { get; set; }
}
