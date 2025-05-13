// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Response;

public class ContributorResponseDTO
{
    public required Person Person { get; init; }
    public List<ContributorRole> Roles { get; init; } = [];
    public List<ContributorPosition> Positions { get; init; } = [];
    public Guid ProjectId { get; init; }

    /// <summary>
    /// True if this contributor is a leader. Multiple leaders are allowed but 1 is required in RAiD
    /// </summary>
    public bool Leader { get; set; }

    /// <summary>
    /// True if this contributor is a contact. Multiple contacts are allowed but 1 is required in RAiD
    /// </summary>
    public bool Contact { get; set; }
}
