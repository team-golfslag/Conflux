// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;

namespace Conflux.Domain.Logic.Services;

public interface IContributorsService
{
    public Task<List<Contributor>> GetContributorsByQueryAsync(Guid projectId, string? query);
    public Task<Contributor> GetContributorByIdAsync(Guid projectId, Guid personId);
    public Task<Contributor> CreateContributorAsync(Guid projectId, ContributorDTO contributorDTO);
    public Task<Contributor> UpdateContributorAsync(Guid projectId, Guid personId, ContributorDTO contributorDTO);
    public Task<Contributor> PatchContributorAsync(Guid projectId, Guid personId, ContributorPatchDTO contributorDTO);
}
