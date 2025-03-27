using Conflux.Domain;

namespace Conflux.API.Results;

/// <summary>
/// Represents the result of a project controller action
/// </summary>
public class ProjectResult
{
    public ProjectResultType ProjectResultType { get; init; }
    public Project? Project { get; init; }
    
    /// <summary>
    /// Constructs the <see cref="ProjectResult"/>
    /// </summary>
    /// <param name="projectResultType">The relevant <see cref="ProjectResultType"/>.</param>
    /// <param name="project">The relevant <see cref="Project"/> if any.</param>
    public ProjectResult(ProjectResultType projectResultType, Project? project)
    {
        ProjectResultType = projectResultType;
        Project = project;
    }
}
/// <summary>
/// Enum listing possible project result states
/// </summary>
public enum ProjectResultType
{
    Success,
    PersonNotFound,
    ProjectNotFound,
    PersonAlreadyAdded,
}