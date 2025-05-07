// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;

namespace Conflux.Domain.Logic.Services;

public interface IContributorsService
{
    public Task<List<ContributorDTO>> GetContributorsByQueryAsync(Guid projectId, string? query);
    public Task<ContributorDTO> GetContributorByIdAsync(Guid projectId, Guid personId);
    public Task<ContributorDTO> CreateContributorAsync(ContributorDTO contributorDTO);
    public Task<ContributorDTO> UpdateContributorAsync(Guid projectId, Guid personId, ContributorDTO contributorDTO);
    public Task<ContributorDTO> PatchContributorAsync(Guid projectId, Guid personId, ContributorPatchDTO contributorDTO);
}
