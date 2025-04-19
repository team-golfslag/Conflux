// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Models;

public class Collaboration
{
    public required string Organization { get; init; }
    public required Group CollaborationGroup { get; init; }
    public required List<Group> Groups { get; init; }
}
