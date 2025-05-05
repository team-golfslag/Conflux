// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for <see cref="Project" /> with POST.
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ProjectDTO
#pragma warning restore S101
{
    public Guid? Id { get; init; }

    public List<ProjectTitleDTO> Titles { get; init; } = [];
    public List<ProjectDescriptionDTO> Descriptions { get; init; } = [];

    public DateTime StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    // TODO: Make this DTO
    // public List<UserDTO> Users { get; init; } = [];

    public List<ContributorDTO> Contributors { get; init; } = [];

    // TODO: Make this DTO
    // public List<ProductDTO> Products { get; init; } = [];

    // TODO: Make this DTO
    // public List<OrganisationDTO> Organisations { get; init; } = [];

    /// <summary>
    /// Converts a <see cref="ProjectDTO" /> to a <see cref="Project" />
    /// </summary>
    /// <returns>The converted <see cref="Project" /></returns>
    public Project ToProject()
    {
        Guid projectId = Guid.NewGuid();
        return new()
        {
            Id = projectId,
            Titles = Titles.ConvertAll(t => t.ToProjectTitle(projectId)),
            Descriptions = Descriptions.ConvertAll(t => t.ToProjectDescription(projectId)),
            StartDate = DateTime.SpecifyKind(StartDate, DateTimeKind.Utc),
            EndDate = EndDate.HasValue ? DateTime.SpecifyKind(EndDate.Value, DateTimeKind.Utc) : null,
        };
    }
}
