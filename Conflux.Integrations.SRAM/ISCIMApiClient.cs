// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.RepositoryConnections.SRAM.Models;

namespace Conflux.RepositoryConnections.SRAM;

/// <summary>
/// Interface for SCIM API client operations
/// </summary>
public interface ISCIMApiClient
{
    /// <summary>
    /// Sets the bearer token for all subsequent requests.
    /// Call this before calling other methods if authentication is required.
    /// </summary>
    void SetBearerToken(string bearerToken);

    /// <summary>
    /// GET /Groups/{id}
    /// Get this SCIM group by ID.
    /// </summary>
    Task<SCIMGroup?> GetSCIMGroup(string id);

    /// <summary>
    /// GET /Groups/
    /// Returns all Groups.
    /// </summary>
    Task<List<SCIMGroup>?> GetAllGroups();

    /// <summary>
    /// GET /Users/{id}
    /// </summary>
    /// <param name="id">The external ID of the user.</param>
    /// <returns>The SCIM user.</returns>
    Task<SCIMUser?> GetSCIMMemberByExternalId(string id);
}
