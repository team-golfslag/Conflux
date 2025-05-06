// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Patch;

public class OrganisationPatchDTO
{
    public string? RORId { get; init; }
    public string? Name { get; init; }
    public List<OrganisationRoleDTO>? Roles { get; init; }
}
