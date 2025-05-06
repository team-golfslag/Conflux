// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class PersonHasContributorsException : Exception
{
    public PersonHasContributorsException(Guid personId)
        : base($"Person with ID {personId} cannot be deleted because they are associated with one or more projects.")
    {
    }
}
