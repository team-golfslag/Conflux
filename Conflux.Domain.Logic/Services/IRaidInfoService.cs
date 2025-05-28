// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Integrations.RAiD;

namespace Conflux.Domain.Logic.Services;

public interface IRaidInfoService
{
    Task<RAiDInfoResponseDTO> GetRAiDInfoByProjectId(Guid projectId);

    Task<List<RAiDIncompatibility>> GetRAiDIncompatibilities(Guid projectId);

    Task<RAiDInfoResponseDTO> MintRAiDAsync(Guid projectId);

    Task<RAiDInfoResponseDTO> SyncRAiDAsync(Guid projectId);
}
