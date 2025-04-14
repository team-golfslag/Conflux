// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a person is already added to a project.
/// </summary>
public class ContributorAlreadyAddedToProjectException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContributorAlreadyAddedToProjectException" /> class.
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="contributorId">The ID of the person that was already added to the project</param>
    public ContributorAlreadyAddedToProjectException(Guid projectId, Guid contributorId)
        : base($"Person with ID {contributorId} was already added to project {projectId}.")
    {
    }
}
