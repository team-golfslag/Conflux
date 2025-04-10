// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Core.Models;

namespace Conflux.Domain.Models;

public class Collaboration
{
    public string Organization { get; set; }
    public Group CollaborationGroup { get; set; }
    public List<Group> Groups { get; set; }
}
