// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

public interface IProjectDescriptionsService
{
    Task<List<ProjectDescriptionResponseDTO>> GetDescriptionsByProjectIdAsync(Guid projectId);
    Task<ProjectDescriptionResponseDTO> GetDescriptionByIdAsync(Guid projectId, Guid descriptionId);

    Task<ProjectDescriptionResponseDTO> CreateDescriptionAsync(Guid projectId,
        ProjectDescriptionRequestDTO descriptionDTO);

    Task<ProjectDescriptionResponseDTO> UpdateDescriptionAsync(Guid projectId, Guid descriptionId,
        ProjectDescriptionRequestDTO descriptionDTO);

    Task DeleteDescriptionAsync(Guid projectId, Guid descriptionId);
}
