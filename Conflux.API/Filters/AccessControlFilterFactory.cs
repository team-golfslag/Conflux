// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Conflux.API.Filters;

/// <summary>
/// Factory for creating AccessControlFilter instances.
/// This allows us to create filter instances through the DI container.
/// </summary>
public class AccessControlFilterFactory(
    IUserSessionService userSessionService,
    IAccessControlService accessControlService)
{
    /// <summary>
    /// Creates an AccessControlFilter with the specified permission.
    /// </summary>
    /// <param name="permission">The permission required for access.</param>
    /// <returns>An instance of AccessControlFilter.</returns>
    public IAsyncAuthorizationFilter Create(UserRoleType permission) =>
        new AccessControlFilter(userSessionService, accessControlService, permission);
}
