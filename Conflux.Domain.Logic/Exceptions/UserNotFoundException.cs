// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException() : base("User not found.")
    {
    }

    public UserNotFoundException(string message) : base(message)
    {
    }
    
    public UserNotFoundException(Guid userId) 
        : base($"User with ID {userId} not found.")
    {
    }

    public UserNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}