// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Conflux.Domain;

/// <summary>
/// Represents a user.
/// </summary>
public record User
{
    [Key] public Guid Id { get; set; }

    [JsonPropertyName("sram_id")] public string? SRAMId { get; set; }

    [JsonPropertyName("scim_id")] public required string SCIMId { get; set; }
    
    public List<UserRole> Roles { get; set; } = [];
    
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.User;
    public List<string> AssignedLectorates { get; set; } = [];
    public List<string> AssignedOrganisations { get; set; } = [];
    
    [Required]
    public required Guid PersonId { get; set; }
    [JsonIgnore]
    public Person? Person { get; set; }
}
