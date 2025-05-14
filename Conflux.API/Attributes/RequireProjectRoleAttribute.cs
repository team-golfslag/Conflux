using Conflux.API.Filters;
using Conflux.Domain;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Conflux.API.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RequireProjectRoleAttribute : Attribute, IFilterFactory
{
    public UserRoleType Permission { get; }
    public bool IsReusable => true;
    public RequireProjectRoleAttribute(UserRoleType role) 
    {
        Permission = role;
    }
    
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var userSessionService = serviceProvider.GetRequiredService<IUserSessionService>();
        var accessControlService = serviceProvider.GetRequiredService<IAccessControlService>();
        return new AccessControlFilter(userSessionService, accessControlService, Permission);
    }
}
