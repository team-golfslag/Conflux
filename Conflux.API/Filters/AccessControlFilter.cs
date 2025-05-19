// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc.Filters;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Filters;

public class AccessControlFilter(
    IUserSessionService userSessionService,
    IAccessControlService accessControlService,
    UserRoleType userRoleType)
    : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        UserSession? userSession = await userSessionService.GetUser();
        if (userSession?.User is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        RouteValueDictionary routeData = context.RouteData.Values;
        Endpoint? endpoint = context.HttpContext.GetEndpoint();
        string? routeParamName = endpoint.Metadata.OfType<RouteParamNameAttribute>().Select(e => e.Name).FirstOrDefault();
        
        // If no RouteParamNameAttribute is found, we'll use a default empty GUID
        // This allows endpoints without project-specific roles to work
        Guid projectId = Guid.Empty;
        
        if (routeParamName != null)
        {
            string? projectIdString = routeData[routeParamName]?.ToString();
            if (projectIdString is null)
            {
                throw new ArgumentNullException(nameof(projectIdString), "Project ID is null");
            }
            if (!Guid.TryParse(projectIdString, out projectId))
            {
                throw new ArgumentException("Project ID is not a valid GUID", nameof(projectIdString));
            }
        }

        Guid userId = userSession.User!.Id;
        
        bool hasRole = await accessControlService.UserHasRoleInProject(userId, projectId, userRoleType);
        if (!hasRole) 
            throw new UnauthorizedAccessException("User does not have the required role in the project");
    }
}
