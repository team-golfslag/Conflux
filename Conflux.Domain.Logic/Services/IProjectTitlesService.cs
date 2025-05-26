// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

public interface IProjectTitlesService
{
    Task<List<ProjectTitleResponseDTO>> GetTitlesByProjectIdAsync(Guid projectId);
    Task<ProjectTitleResponseDTO> GetTitleByIdAsync(Guid projectId, Guid titleId);

    Task<ProjectTitleResponseDTO?> GetCurrentTitleByTitleType(Guid projectId, TitleType titleType);

    Task<List<ProjectTitleResponseDTO>> CreateTitleAsync(Guid projectId,
        ProjectTitleRequestDTO titleDTO);

    Task<ProjectTitleResponseDTO> EndTitleAsync(Guid projectId, Guid titleId);

    Task<ProjectTitleResponseDTO> UpdateTitleAsync(Guid projectId, Guid titleId,
        ProjectTitleRequestDTO titleDTO);

    Task DeleteTitleAsync(Guid projectId, Guid titleId);
}
