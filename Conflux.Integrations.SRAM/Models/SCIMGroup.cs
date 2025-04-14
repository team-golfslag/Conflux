using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMGroup
{
    [JsonPropertyName("displayName")] public string DisplayName { get; init; }

    [JsonPropertyName("externalId")] public string ExternalId { get; init; }

    [JsonPropertyName("id")] public string Id { get; init; }

    [JsonPropertyName("members")] public List<SCIMMember> Members { get; init; }

    [JsonPropertyName("meta")] public SCIMMeta SCIMMeta { get; init; }

    [JsonPropertyName("schemas")] public List<string> Schemas { get; init; }

    [JsonPropertyName("urn:mace:surf.nl:sram:scim:extension:Group")]
    public SCIMGroupInfo SCIMGroupInfo { get; init; }
}
