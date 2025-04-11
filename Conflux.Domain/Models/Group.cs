// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Models;

public class Group
{
    public required string Id { get; set; }
    public required string Urn { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? LogoUrl { get; set; }
    public required string ExternalId { get; set; }
    public required string SRAMId { get; set; }

    public List<GroupMember> Members { get; set; } = [];
}
