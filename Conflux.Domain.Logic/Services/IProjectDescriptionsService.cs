// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// Interface for the project descriptions service.
/// </summary>
public interface IProjectDescriptionsService
{
    /// <summary>
    /// Gets all descriptions for a project.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <returns>A list of project descriptions.</returns>
    Task<List<ProjectDescriptionResponseDTO>> GetDescriptionsByProjectIdAsync(Guid projectId);

    /// <summary>
    /// Gets a description by its GUID.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="descriptionId"> The description id.</param>
    /// <returns>The description.</returns>
    Task<ProjectDescriptionResponseDTO> GetDescriptionByIdAsync(Guid projectId, Guid descriptionId);

    /// <summary>
    /// Creates a new description for a project.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="descriptionDTO">The description data.</param>
    /// <returns>The created description.</returns>
    Task<ProjectDescriptionResponseDTO> CreateDescriptionAsync(Guid projectId,
        ProjectDescriptionRequestDTO descriptionDTO);

    /// <summary>
    /// Updates an existing description for a project.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="descriptionId">The description id.</param>
    /// <param name="descriptionDTO">The description data.</param>
    /// <returns>The updated description.</returns>
    Task<ProjectDescriptionResponseDTO> UpdateDescriptionAsync(Guid projectId, Guid descriptionId,
        ProjectDescriptionRequestDTO descriptionDTO);

    /// <summary>
    /// Deletes a description for a project.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="descriptionId">The description id.</param>
    /// <returns>The task.</returns>
    Task DeleteDescriptionAsync(Guid projectId, Guid descriptionId);
}
