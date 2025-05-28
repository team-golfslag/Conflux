// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class ProjectAlreadyHasOrganisationException : Exception
{
    public ProjectAlreadyHasOrganisationException(Guid projectId, Guid organisationId)
        : base($"Project with ID {projectId} already has an organisation with ID {organisationId}.")
    {
    }
}
