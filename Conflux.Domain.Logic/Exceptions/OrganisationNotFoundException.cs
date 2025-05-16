// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class OrganisationNotFoundException : Exception
{
    public OrganisationNotFoundException(string? message) : base(message)
    {
    }

    public OrganisationNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public OrganisationNotFoundException(Guid organisationId) : this(
        $"Organisation with ID {organisationId} not found.")
    {
    }
}
