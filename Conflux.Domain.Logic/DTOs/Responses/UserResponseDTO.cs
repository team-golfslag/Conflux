// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.Domain.Logic.DTOs.Responses;

public class UserResponseDTO
{
    public Guid Id { get; init; }
    
    [JsonPropertyName("sram_id")] public string? SRAMId { get; init; }
    [JsonPropertyName("scim_id")] public required string SCIMId { get; set; }
    public List<UserRole> Roles { get; set; } = [];
    
    public PersonResponseDTO? Person { get; init; }
}
