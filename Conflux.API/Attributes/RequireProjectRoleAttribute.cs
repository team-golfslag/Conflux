// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Filters;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Conflux.API.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RequireProjectRoleAttribute : Attribute, IFilterFactory
{
    public RequireProjectRoleAttribute(UserRoleType role)
    {
        Permission = role;
    }

    public UserRoleType Permission { get; }

    // Set to false to ensure we get fresh services for each request
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        // Get the filter factory from the DI container
        AccessControlFilterFactory filterFactory = serviceProvider.GetRequiredService<AccessControlFilterFactory>();

        // Create a filter with the specified permission
        return filterFactory.Create(Permission);
    }
}
