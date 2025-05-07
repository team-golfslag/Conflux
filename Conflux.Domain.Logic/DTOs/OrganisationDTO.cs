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

    public Organisation ToOrganisation() =>
        new()
        {
            RORId = RORId,
            Name = Name,
        };
}
