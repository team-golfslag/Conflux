// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class ProjectOrganisationNotFoundException : Exception
{
    public ProjectOrganisationNotFoundException(Guid projectId, Guid organisationId)
        : base($"Project organisation with Project ID {projectId} and Organisation ID {organisationId} not found.")
    {
    }
}
