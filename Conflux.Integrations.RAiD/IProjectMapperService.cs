// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using RAiD.Net.Domain;

namespace Conflux.Integrations.RAiD;

public interface IProjectMapperService
{
    public RAiDCreateRequest MapProjectCreationRequest(Project project);
}
