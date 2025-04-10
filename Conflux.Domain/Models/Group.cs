// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Models;

namespace Conflux.Core.Models;

public class Group
{
    public string Id { get; set; }
    public string Urn { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string? Url { get; set; }
    public string? LogoUrl { get; set; }
    public string ExternalId { get; set; }
    public string SRAMId { get; set; }

    public List<GroupMember> Members { get; set; }
}
