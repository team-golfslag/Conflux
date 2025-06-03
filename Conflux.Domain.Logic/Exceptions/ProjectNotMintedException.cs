// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.Exceptions;

public class ProjectNotMintedException(Guid projectId)
    : Exception($"Project with id {projectId} has not been minted.")
{
}
