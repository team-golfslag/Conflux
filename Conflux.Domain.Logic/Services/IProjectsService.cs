// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Queries;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

public interface IProjectsService
{
    public Task<List<UserRole>?> GetRolesFromProject(Project project);
    public Task FavoriteProjectAsync(Guid projectId, bool favorite);

    public Task<ProjectResponseDTO> GetProjectDTOByIdAsync(Guid id);
    public Task<Project> GetProjectByIdAsync(Guid id);

    public Task<List<ProjectResponseDTO>> GetProjectsByQueryAsync(ProjectQueryDTO dto);

    public Task<string> ExportProjectsToCsvAsync(ProjectCsvRequestDTO dto);

    public Task<List<ProjectResponseDTO>> GetAllProjectsAsync();

    public Task<ProjectResponseDTO> PutProjectAsync(Guid id, ProjectRequestDTO dto);
    public Task<bool> UpdateProjectEmbeddingAsync(Guid projectId);
    public Task<int> UpdateProjectEmbeddingsAsync();
    public Task<int> RecomputeAllProjectEmbeddingsAsync();
}
