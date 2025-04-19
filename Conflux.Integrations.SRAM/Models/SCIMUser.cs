// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.RepositoryConnections.SRAM.Models;

public class SCIMUser
{
    public required string Id { get; set; }
    public string? ExternalId { get; set; }
    public string? UserName { get; set; }
    public SCIMName? Name { get; set; }
    public string? DisplayName { get; set; }
    public List<string>? Schemas { get; set; }
    public List<SCIMEmail>? Emails { get; set; }
}
