// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

public interface IContributorsService
{
    public Task<List<ContributorResponseDTO>> GetContributorsByQueryAsync(Guid projectId, string? query);
    public Task<ContributorResponseDTO> GetContributorByIdAsync(Guid projectId, Guid personId);

    public Task<ContributorResponseDTO> CreateContributorAsync(Guid projectId, Guid personId,
        ContributorRequestDTO contributorDTO);

    public Task<ContributorResponseDTO> UpdateContributorAsync(Guid projectId, Guid personId,
        ContributorRequestDTO contributorDTO);

    public Task DeleteContributorAsync(Guid projectId, Guid personId);
}
