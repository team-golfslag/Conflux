// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class PersonIsAssociatedWithUserException : Exception
{
    public PersonIsAssociatedWithUserException() : base("The person is associated with a user and cannot be deleted.")
    {
    }

    public PersonIsAssociatedWithUserException(string message) : base(message)
    {
    }

    public PersonIsAssociatedWithUserException(string message, Exception innerException) : base(message, innerException)
    {
    }
}