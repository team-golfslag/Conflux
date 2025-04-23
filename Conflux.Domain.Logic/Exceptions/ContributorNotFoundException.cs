// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a contributor is not found.
/// </summary>
public class ContributorNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContributorNotFoundException" /> class.
    /// </summary>
    /// <param name="contributorId">The ID of the contributor that was not found</param>
    public ContributorNotFoundException(Guid contributorId)
        : base($"User with ID {contributorId} was not found.")
    {
    }
}
