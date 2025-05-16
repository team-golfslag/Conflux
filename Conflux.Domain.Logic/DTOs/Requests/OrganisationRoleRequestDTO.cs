// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Requests;

public class OrganisationRoleRequestDTO
{
    public required OrganisationRoleType Role { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}
