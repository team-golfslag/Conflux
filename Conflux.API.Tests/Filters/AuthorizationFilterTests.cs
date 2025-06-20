// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.Domain;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Conflux.API.Filters;

/// <summary>
/// Authorization filter for integration testing
/// </summary>
public class AuthorizationFilterTests : IAsyncAuthorizationFilter
{
    private readonly IAccessControlService _accessControlService;
    private readonly IUserSessionService _userSessionService;

    public AuthorizationFilterTests(
        IUserSessionService userSessionService,
        IAccessControlService accessControlService)
    {
        _userSessionService = userSessionService;
        _accessControlService = accessControlService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get metadata from endpoint
        Endpoint? endpoint = context.HttpContext.GetEndpoint();

        // Check for access control attribute
        AccessControlFilterAttributeTests? accessControlAttribute =
            endpoint?.Metadata.GetMetadata<AccessControlFilterAttributeTests>();
        if (accessControlAttribute == null)
            // No access control required
            return;

        // Check user authentication
        User user = await _userSessionService.GetUser();
        
        // Check route parameter for project ID
        RouteValueDictionary routeData = context.RouteData.Values;
        RouteParamNameAttribute? routeParamNameAttribute = endpoint?.Metadata.GetMetadata<RouteParamNameAttribute>();
        string? routeParamName = routeParamNameAttribute?.Name;

        // If no RouteParamNameAttribute is found, we'll use a default empty GUID
        Guid projectId = Guid.Empty;

        if (routeParamName != null)
        {
            string? projectIdString = routeData[routeParamName]?.ToString();
            if (projectIdString is null) throw new ArgumentNullException(nameof(projectIdString), "Project ID is null");
            if (!Guid.TryParse(projectIdString, out projectId))
                throw new ArgumentException("Project ID is not a valid GUID", nameof(projectIdString));
        }

        Guid userId = user.Id;

        // Check if user has the required role
        bool hasRole = await _accessControlService.UserHasRoleInProject(
            userId, projectId, accessControlAttribute.RequiredRole);

        if (!hasRole) context.Result = new ForbidResult();
    }
}
