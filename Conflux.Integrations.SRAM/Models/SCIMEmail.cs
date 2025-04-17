// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.RepositoryConnections.SRAM.Models;

public abstract class SCIMEmail
{
    public string? Value { get; set; }
    public string? Type { get; set; }
    public bool Primary { get; set; }
}
