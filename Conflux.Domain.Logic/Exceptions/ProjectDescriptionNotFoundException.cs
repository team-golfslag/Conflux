// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class ProjectDescriptionNotFoundException : Exception
{
    public ProjectDescriptionNotFoundException(Guid descriptionId)
        : base($"Project description with ID {descriptionId} not found.")
    {
    }
}
