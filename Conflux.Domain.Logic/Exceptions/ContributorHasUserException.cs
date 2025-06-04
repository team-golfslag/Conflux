// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class ContributorHasUserException : Exception
{
    public ContributorHasUserException() : base("The contributor is associated with a user and cannot be deleted.")
    {
    }

    public ContributorHasUserException(string message) : base(message)
    {
    }
}