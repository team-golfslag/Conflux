// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Session;

public class GroupMember
{
    public required string DisplayName { get; set; }
    public required string SCIMId { get; set; }
}
