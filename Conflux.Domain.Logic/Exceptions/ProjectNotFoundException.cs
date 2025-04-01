// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a project is not found.
/// </summary>
public class ProjectNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectNotFoundException" /> class.
    /// </summary>
    /// <param name="projectId">The ID of the project that was not found</param>
    public ProjectNotFoundException(Guid projectId)
        : base($"Project with ID {projectId} was not found.")
    {
    }
}
