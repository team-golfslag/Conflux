namespace Conflux.Domain.Logic.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a person is already added to a project.
/// </summary>
public class PersonAlreadyAddedToProjectException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersonAlreadyAddedToProjectException" /> class.
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="personId">The ID of the person that was already added to the project</param>
    public PersonAlreadyAddedToProjectException(Guid projectId, Guid personId)
        : base($"Person with ID {personId} was already added to project {projectId}.")
    {
    }
}
