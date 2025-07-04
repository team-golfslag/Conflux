// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Responses;

public class ProjectResponseDTO
{
    public required Guid Id { get; init; }
    public List<ProjectTitleResponseDTO> Titles { get; init; } = [];
    public List<ProjectDescriptionResponseDTO> Descriptions { get; init; } = [];

    public DateTime StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public List<UserResponseDTO> Users { get; set; } = [];

    public List<ContributorResponseDTO> Contributors { get; init; } = [];

    public List<ProductResponseDTO> Products { get; init; } = [];

    public List<ProjectOrganisationResponseDTO> Organisations { get; init; } = [];

    public string? Lectorate { get; init; }
    
    public string? OwnerOrganisation { get; init; }
}
