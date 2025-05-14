// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc.Filters;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Filters;

public class AccessControlFilter : IAsyncAuthorizationFilter
{
    private readonly IUserSessionService _userSessionService;
    private readonly IAccessControlService _userAccessControlService;
    private readonly UserRoleType _userRoleType;

    public AccessControlFilter(IUserSessionService userSessionService, IAccessControlService accessControlService, UserRoleType userRoleType)
    {
        _userSessionService = userSessionService;
        _userAccessControlService = accessControlService;
        _userRoleType = userRoleType;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var userSession = await _userSessionService.GetUser();
        if (userSession is null || userSession.User is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var routeData = context.RouteData.Values;
        var endpoint = context.HttpContext.GetEndpoint();
        var routeParamName = endpoint.Metadata.OfType<RouteParamNameAttribute>().Select(e => e.Name).FirstOrDefault();
        if (routeParamName is null)
        {
            throw new ArgumentNullException(nameof(routeParamName), "Route parameter name is null");
        }
        var projectIdString = routeData[routeParamName]?.ToString();
        if (projectIdString is null)
        {
            throw new ArgumentNullException(nameof(projectIdString), "Project ID is null");
        }
        if (!Guid.TryParse(projectIdString, out var projectId))
        {
            throw new ArgumentException("Project ID is not a valid GUID", nameof(projectIdString));
        }

        var userId = userSession.User!.Id;
        
        var hasRole = await _userAccessControlService.UserHasRoleInProject(userId, projectId, _userRoleType);
        if (!hasRole)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
