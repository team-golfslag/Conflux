// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Responses;

public class ContributorResponseDTO
{
    public required Person Person { get; init; }
    public List<ContributorRoleResponseDTO> Roles { get; init; } = [];
    public List<ContributorPositionResponseDTO> Positions { get; init; } = [];
    public Guid ProjectId { get; init; }

    /// <summary>
    /// True if this contributor is a leader. Multiple leaders are allowed but 1 is required in RAiD
    /// </summary>
    public bool Leader { get; init; }

    /// <summary>
    /// True if this contributor is a contact. Multiple contacts are allowed but 1 is required in RAiD
    /// </summary>
    public bool Contact { get; init; }
}
