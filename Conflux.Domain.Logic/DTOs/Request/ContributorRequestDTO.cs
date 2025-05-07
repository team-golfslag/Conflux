// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Request;

public class ContributorRequestDTO
{
    public bool Leader { get; set; }
    public bool Contact { get; set; }
    public List<ContributorRoleType> Roles { get; set; } = [];
    public List<ContributorPositionRequestDTO> Positions { get; set; } = [];
}
