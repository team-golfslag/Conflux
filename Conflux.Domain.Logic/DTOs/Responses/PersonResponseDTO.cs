// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.Domain.Logic.DTOs.Responses;

public class PersonResponseDTO
{
    public Guid Id { get; init; }

    [JsonPropertyName("orcid_id")] public string? ORCiD { get; set; }
    public required string Name { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Email { get; set; }
}
