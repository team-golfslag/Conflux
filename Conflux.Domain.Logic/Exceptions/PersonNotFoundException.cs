namespace Conflux.Domain.Logic.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a person is not found.
/// </summary>
public class PersonNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersonNotFoundException" /> class.
    /// </summary>
    /// <param name="personId">The ID of the person that was not found</param>
    public PersonNotFoundException(Guid personId)
        : base($"Person with ID {personId} was not found.")
    {
    }
}
