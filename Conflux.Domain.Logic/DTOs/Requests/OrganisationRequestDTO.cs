// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Requests;

public class OrganisationRequestDTO
{
    public OrganisationRoleType? Role { get; init; }
    public string? RORId { get; init; }
    public required string Name { get; init; }
}