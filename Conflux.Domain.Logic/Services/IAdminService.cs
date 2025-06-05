// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

public interface IAdminService
{
    /// <summary>
    /// Retrieves a list of users based on a query string and an option to filter for admins only.
    /// Requires SuperAdmin privileges.
    /// </summary>
    /// <param name="query">The search query to filter users by name or email. Can be null or empty to retrieve all users (respecting adminsOnly filter).</param>
    /// <param name="adminsOnly">A boolean indicating whether to return only users with SystemAdmin or SuperAdmin permission levels.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="UserResponseDTO"/>.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the current user does not have SuperAdmin privileges.</exception>
    public Task<List<UserResponseDTO>> GetUsersByQuery(string? query, bool adminsOnly);

    /// <summary>
    /// Sets the permission level for a specified user.
    /// Requires SuperAdmin privileges. Cannot set permission level to SuperAdmin.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose permission level is to be set.</param>
    /// <param name="permissionLevel">The new permission level to assign to the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see cref="UserResponseDTO"/>.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the current user does not have SuperAdmin privileges.</exception>
    /// <exception cref="ArgumentException">Thrown if attempting to set permission level to SuperAdmin.</exception>
    /// <exception cref="Exception">Thrown if the user with the specified ID is not found.</exception>
    public Task<UserResponseDTO> SetUserPermissionLevel(Guid userId, PermissionLevel permissionLevel);

    /// <summary>
    /// Retrieves a list of available lectorates from the application configuration.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of strings representing available lectorates.</returns>
    public Task<List<string>> GetAvailableLectorates();

    /// <summary>
    /// Retrieves a list of unique organisations from all projects.
    /// Requires SuperAdmin privileges.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of strings representing available organisations.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the current user does not have SuperAdmin privileges.</exception>
    public Task<List<string>> GetAvailableOrganisations();

    /// <summary>
    /// Assigns a list of lectorates to a specified user.
    /// Requires SuperAdmin privileges.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to whom lectorates will be assigned.</param>
    /// <param name="lectorates">A list of strings representing the lectorates to assign.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see cref="UserResponseDTO"/>.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the current user does not have SuperAdmin privileges.</exception>
    /// <exception cref="Exception">Thrown if the user with the specified ID is not found.</exception>
    public Task<UserResponseDTO> AssignLectoratesToUser(Guid userId, List<string> lectorates);

    /// <summary>
    /// Assigns a list of organisations to a specified user.
    /// Requires SuperAdmin privileges.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to whom organisations will be assigned.</param>
    /// <param name="organisations">A list of strings representing the organisations to assign.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see cref="UserResponseDTO"/>.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the current user does not have SuperAdmin privileges.</exception>
    /// <exception cref="Exception">Thrown if the user with the specified ID is not found.</exception>
    public Task<UserResponseDTO> AssignOrganisationsToUser(Guid userId, List<string> organisations);
}

