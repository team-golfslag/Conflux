// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs;

public class ProjectOrganisationDTO
{
    public Guid ProjectId { get; init; }
    public Guid OrganisationId { get; init; }
    public List<OrganisationRoleRequestDTO> Roles { get; init; } = [];

    public ProjectOrganisation ToProjectOrganisation() =>
        new()
        {
            ProjectId = ProjectId,
            OrganisationId = OrganisationId,
            Roles = Roles.Select(role => role.ToOrganisationRole(OrganisationId)).ToList(),
        };
}
