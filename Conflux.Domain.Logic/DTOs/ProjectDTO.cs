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
    public required Guid Id { get; init; }
    public ProjectTitleDTO? PrimaryTitle { get; init; }
    public List<ProjectTitleDTO> Titles { get; init; } = [];
    public ProjectDescriptionDTO? PrimaryDescription { get; init; }
    public List<ProjectDescriptionDTO> Descriptions { get; init; } = [];

    public DateTime StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public List<UserDTO> Users { get; init; } = [];

    public List<ContributorDTO> Contributors { get; init; } = [];

    public List<ProductDTO> Products { get; init; } = [];

    public List<OrganisationDTO> Organisations { get; init; } = [];

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
            Users = Users.ConvertAll(u => u.ToUser(projectId)),
            Products = Products.ConvertAll(p => p.ToProduct()),
            Organisations = Organisations.ConvertAll(o => o.ToOrganisation()),
            Contributors = Contributors.ConvertAll(c => c.ToContributor()),
        };
    }
}
