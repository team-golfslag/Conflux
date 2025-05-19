using Conflux.API.Filters;
using Conflux.Domain;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Conflux.API.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RequireProjectRoleAttribute : Attribute, IFilterFactory
{
    public UserRoleType Permission { get; }
    
    // Set to false to ensure we get fresh services for each request
    public bool IsReusable => false;
    
    public RequireProjectRoleAttribute(UserRoleType role) 
    {
        Permission = role;
    }
    
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        // Get the filter factory from the DI container
        var filterFactory = serviceProvider.GetRequiredService<AccessControlFilterFactory>();
        
        // Create a filter with the specified permission
        return filterFactory.Create(Permission);
    }
}
