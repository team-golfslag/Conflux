// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;
using Conflux.Domain.Session;

namespace Conflux.Domain.Logic.DTOs.Responses;

public class UserSessionResponseDTO
{
    [JsonPropertyName("sram_id")]
    public string SRAMId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserResponseDTO? User { get; set; }
    public List<Collaboration> Collaborations { get; set; } = [];
}
