// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Filters;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Conflux.API.Attributes;

/// <summary>
/// Specifies that access to a controller action or controller requires a specific project role.
/// </summary>
/// <remarks>
/// This attribute is used to enforce role-based access control for project-specific endpoints.
/// When applied to a controller or action method, it ensures that the current user has the specified 
/// role for the project identified by the route parameter.
/// 
/// Use this attribute in conjunction with <see cref="RouteParamNameAttribute"/> to specify 
/// which route parameter contains the project identifier.
/// </remarks>
/// <example>
/// Usage on a controller method:
/// <code>
/// [HttpGet("{projectId}/details")]
/// [RequireProjectRole(UserRoleType.Admin)]
/// [RouteParamName("projectId")]
/// public IActionResult GetProjectDetails(Guid projectId)
/// {
///     // Method implementation - only accessible to project admins
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RequireProjectRoleAttribute(UserRoleType role) : Attribute, IFilterFactory
{
    public UserRoleType Permission { get; } = role;

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
