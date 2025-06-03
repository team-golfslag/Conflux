// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using RAiD.Net.Domain;

namespace Conflux.Integrations.RAiD;

public interface IProjectMapperService
{
    public Task<RAiDCreateRequest> MapProjectCreationRequest(Guid projectId);

    public Task<RAiDUpdateRequest> MapProjectUpdateRequest(Guid projectId);
    public Task<List<RAiDIncompatibility>> CheckProjectCompatibility(Guid projectId);
}
