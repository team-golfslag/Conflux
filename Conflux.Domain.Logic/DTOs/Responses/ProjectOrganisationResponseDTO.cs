// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Responses;

public class ProjectOrganisationResponseDTO
{
    public Guid ProjectId { get; init; }
    public OrganisationResponseDTO Organisation { get; init; }
}
