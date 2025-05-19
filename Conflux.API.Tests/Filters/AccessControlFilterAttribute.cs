// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;

namespace Conflux.API.Filters;

/// <summary>
/// Marker attribute for integration testing to specify required roles
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AccessControlFilterAttribute : Attribute
{
    public UserRoleType RequiredRole { get; }

    public AccessControlFilterAttribute(UserRoleType requiredRole = UserRoleType.Admin)
    {
        RequiredRole = requiredRole;
    }
}
