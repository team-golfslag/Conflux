// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.RepositoryConnections.SRAM.DTOs;

public record CollaborationDTO
{
    public string Name { get; init; } = string.Empty;
    public string Organization { get; init; } = string.Empty;
    public List<string> Groups { get; init; } = [];
}
