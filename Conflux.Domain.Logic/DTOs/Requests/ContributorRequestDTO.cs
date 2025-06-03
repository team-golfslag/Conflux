// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Requests;

public class ContributorRequestDTO
{
    public bool Leader { get; init; }
    public bool Contact { get; init; }
    public List<ContributorRoleType> Roles { get; init; } = [];
    public ContributorPositionType? Position { get; init; }
}
