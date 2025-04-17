// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.RepositoryConnections.SRAM.Models;

public abstract class SCIMUser
{
    public required string Id { get; init; }
    public string? ExternalId { get; init; }
    public string? UserName { get; init; }
    public SCIMName? Name { get; init; }
    public string? DisplayName { get; init; }
    public List<string>? Schemas { get; init; }
    public List<SCIMEmail>? Emails { get; init; }
}
