// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs;

public class OrganisationDTO
{
    public Guid? Id { get; init; }
    public string? RORId { get; init; }
    public required string Name { get; init; }
    public List<OrganisationRoleDTO> Roles { get; init; } = [];

    public Organisation ToOrganisation()
    {
        return new()
        {
            RORId = RORId,
            Name = Name,
            Roles = Roles.Select(role => role.ToOrganisationRole(Id ?? Guid.NewGuid())).ToList(),
        };
    }
}
