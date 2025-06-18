// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.Domain;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Conflux.API.Filters;

public class AccessControlFilter(
    IUserSessionService userSessionService,
    IAccessControlService accessControlService,
    UserRoleType userRoleType)
    : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        User user = await userSessionService.GetUser();

        RouteValueDictionary routeData = context.RouteData.Values;
        Endpoint? endpoint = context.HttpContext.GetEndpoint();
        string? routeParamName =
            endpoint.Metadata.OfType<RouteParamNameAttribute>().Select(e => e.Name).FirstOrDefault();

        // If no RouteParamNameAttribute is found, we'll use a default empty GUID
        // This allows endpoints without project-specific roles to work
        Guid projectId = Guid.Empty;

        if (routeParamName != null)
        {
            string? projectIdString = routeData[routeParamName]?.ToString();
            if (projectIdString is null) throw new ArgumentNullException(nameof(projectIdString), "Project ID is null");
            if (!Guid.TryParse(projectIdString, out projectId))
                throw new ArgumentException("Project ID is not a valid GUID", nameof(projectIdString));
        }
        
        bool hasRole = await accessControlService.UserHasRoleInProject(user.Id, projectId, userRoleType);
        if (!hasRole)
            throw new UnauthorizedAccessException("User does not have the required role in the project");
    }
}
