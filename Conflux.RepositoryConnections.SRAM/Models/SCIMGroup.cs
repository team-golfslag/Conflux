using System.Text.Json.Serialization;

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMGroup
{
    [JsonPropertyName("displayName")] public string DisplayName { get; set; }

    [JsonPropertyName("externalId")] public string ExternalId { get; set; }

    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("members")] public List<SCIMMember> Members { get; set; }

    [JsonPropertyName("meta")] public SCIMMeta SCIMMeta { get; set; }

    [JsonPropertyName("schemas")] public List<string> Schemas { get; set; }

    [JsonPropertyName("urn:mace:surf.nl:sram:scim:extension:Group")]
    public SCIMGroupInfo SCIMGroupInfo { get; set; }
}
