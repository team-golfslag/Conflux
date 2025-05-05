// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Patch;

/// <summary>
/// The Data Transfer Object for updating a <see cref="Project" /> with PATCH.
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ProjectPatchDTO
#pragma warning restore S101
{
    public List<ProjectTitleDTO>? Titles { get; init; }
    public List<ProjectDescriptionDTO>? Descriptions { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }
}
