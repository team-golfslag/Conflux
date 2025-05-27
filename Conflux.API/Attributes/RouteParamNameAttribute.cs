// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.API.Attributes;

/// <summary>
/// Specifies the name of the route parameter that contains the project identifier for access control.
/// </summary>
/// <remarks>
/// This attribute is used in conjunction with the <see cref="Filters.AccessControlFilter" /> to identify
/// which route parameter contains the project ID needed for role-based access control checks.
/// Apply this attribute to controller methods or classes where project-specific
/// role-based authorization is required.
/// </remarks>
/// <example>
/// Usage on a controller method:
/// <code>
/// [HttpGet("{projectId}/details")]
/// [RouteParamName("projectId")]
/// public IActionResult GetProjectDetails(Guid projectId)
/// {
///     // Method implementation
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RouteParamNameAttribute(string routeParamName) : Attribute
{
    public string Name { get; } = routeParamName;
}
