using Microsoft.AspNetCore.Mvc;

namespace Conflux.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RouteParamNameAttribute(string routeParamName) : Attribute
{
    public string Name { get; } = routeParamName;
}
