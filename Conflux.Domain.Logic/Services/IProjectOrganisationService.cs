// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// Interface for the project organisations service
/// </summary>
public interface IProjectOrganisationsService
{
    /// <summary>
    /// Gets all organisations for a project
    /// </summary>
    /// <param name="projectId">The project id</param>
    /// <returns>A list of organisations</returns>
    Task<List<ProjectOrganisationResponseDTO>> GetOrganisationsByProjectIdAsync(Guid projectId);

    /// <summary>
    /// Gets an organisation by its ID
    /// </summary>
    /// <param name="projectId">The project id</param>
    /// <param name="organisationId">The organisation id</param>
    /// <returns>The organisation</returns>
    Task<ProjectOrganisationResponseDTO> GetOrganisationByIdAsync(Guid projectId, Guid organisationId);

    /// <summary>
    /// Creates a new organisation for a project
    /// </summary>
    /// <param name="projectId">The project id</param>
    /// <param name="organisationDto">The organisation data</param>
    /// <returns>The created organisation</returns>
    Task<ProjectOrganisationResponseDTO>
        CreateOrganisationAsync(Guid projectId, OrganisationRequestDTO organisationDto);

    /// <summary>
    /// Updates an existing organisation for a project
    /// </summary>
    /// <param name="projectId">The project id</param>
    /// <param name="organisationId">The organisation id</param>
    /// <param name="organisationDto">The organisation data</param>
    /// <returns>The updated organisation</returns>
    Task<ProjectOrganisationResponseDTO> UpdateOrganisationAsync(Guid projectId, Guid organisationId,
        OrganisationRequestDTO organisationDto);

    /// <summary>
    /// Deletes an organisation from a project
    /// </summary>
    /// <param name="projectId">The project id</param>
    /// <param name="organisationId">The organisation id</param>
    Task DeleteOrganisationAsync(Guid projectId, Guid organisationId);
    
    Task<OrganisationResponseDTO> GetOrganisationNameByRorAsync(string ror);
    
    Task<List<OrganisationResponseDTO>> FindOrganisationsByName(string query);
}
