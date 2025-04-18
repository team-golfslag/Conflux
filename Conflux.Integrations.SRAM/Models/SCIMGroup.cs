// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMGroup
{
    [JsonPropertyName("displayName")] public required string DisplayName { get; init; }

    [JsonPropertyName("externalId")] public required string ExternalId { get; init; }

    [JsonPropertyName("id")] public required string Id { get; init; }

    [JsonPropertyName("members")] public required List<SCIMMember> Members { get; init; }

    [JsonPropertyName("meta")] public required SCIMMeta SCIMMeta { get; init; }

    [JsonPropertyName("schemas")] public required List<string> Schemas { get; init; }

    [JsonPropertyName("urn:mace:surf.nl:sram:scim:extension:Group")]
    public required SCIMGroupInfo SCIMGroupInfo { get; init; }
}
