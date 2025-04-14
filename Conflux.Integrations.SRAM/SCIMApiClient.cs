// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net.Http.Json;
using Conflux.RepositoryConnections.SRAM.Models;

namespace Conflux.RepositoryConnections.SRAM;

public class SCIMApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Pass a configured HttpClient (with BaseAddress set) into the constructor.
    /// Example usage:
    /// <code>
    ///   var httpClient = new HttpClient
    ///   {
    ///       BaseAddress = new Uri("https://YOUR-API-ENDPOINT/")
    ///   };
    ///   var client = new SCIMApiClient(httpClient);
    /// </code>
    /// </summary>
    public SCIMApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Sets the bearer token for all subsequent requests.
    /// Call this before calling other methods if authentication is required.
    /// </summary>
    public void SetBearerToken(string bearerToken) =>
        _httpClient.DefaultRequestHeaders.Authorization =
            new("Bearer", bearerToken);


    /// <summary>
    /// GET /Groups/{id}
    /// Get this SCIM group by ID.
    /// </summary>
    public async Task<SCIMGroup?> GetSCIMGroup(string id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"Groups/{id}");
        response.EnsureSuccessStatusCode();
        SCIMGroup? result = await response.Content.ReadFromJsonAsync<SCIMGroup>();

        return result;
    }

    /// <summary>
    /// GET /Groups/
    /// Returns all Groups.
    /// </summary>
    public async Task<List<SCIMGroup>?> GetAllGroups()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("Groups");
        response.EnsureSuccessStatusCode();
        SCIMGroupsResult? results = await response.Content.ReadFromJsonAsync<SCIMGroupsResult>();
        return results?.Groups;
    }

    /// <summary>
    /// GET /Users/{id}
    /// </summary>
    /// <param name="id">The external ID of the user.</param>
    /// <returns>The SCIM user.</returns>
    public async Task<SCIMUser?> GetSCIMMemberByExternalId(string id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"Users/{id}");
        response.EnsureSuccessStatusCode();
        SCIMUser? result = await response.Content.ReadFromJsonAsync<SCIMUser>();

        return result;
    }
}
