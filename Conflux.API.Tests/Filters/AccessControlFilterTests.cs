// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.API.Filters;
using Conflux.Domain;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Conflux.API.Tests.Filters;

public class AccessControlFilterTests
{
    [Fact]
    public async Task OnAuthorizationAsync_UserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var userSessionService = new Mock<IUserSessionService>();
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync((UserSession?)null);
        
        var accessControlService = new Mock<IAccessControlService>();
        var filter = new AccessControlFilter(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);
        
        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        
        // Act
        await filter.OnAuthorizationAsync(context);
        
        // Assert
        Assert.IsType<UnauthorizedResult>(context.Result);
    }
    
    [Fact]
    public async Task OnAuthorizationAsync_UserHasNoRole_ReturnsForbid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        
        var userSessionService = new Mock<IUserSessionService>();
        var userSession = new UserSession
        {
            User = new User { Id = userId, Name = "Test User", SCIMId = "test-scim-id" },
            Collaborations = new List<Collaboration>()
        };
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync(userSession);
        
        var accessControlService = new Mock<IAccessControlService>();
        accessControlService.Setup(x => x.UserHasRoleInProject(userId, projectId, UserRoleType.Admin))
            .ReturnsAsync(false);
        
        var filter = new AccessControlFilter(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);
        
        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };
        
        // Add a route parameter for the project ID
        actionContext.RouteData.Values["id"] = projectId.ToString();
        
        // Add the RouteParamNameAttribute to the endpoint metadata
        var metadata = new List<object> { new RouteParamNameAttribute("id") };
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(metadata), null);
        
        actionContext.HttpContext.SetEndpoint(endpoint);
        
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        
        // Act
        await filter.OnAuthorizationAsync(context);
        
        // Assert
        Assert.IsType<ForbidResult>(context.Result);
    }
    
    [Fact]
    public async Task OnAuthorizationAsync_UserHasRole_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        
        var userSessionService = new Mock<IUserSessionService>();
        var userSession = new UserSession
        {
            User = new User { Id = userId, Name = "Test User", SCIMId = "test-scim-id" },
            Collaborations = new List<Collaboration>()
        };
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync(userSession);
        
        var accessControlService = new Mock<IAccessControlService>();
        accessControlService.Setup(x => x.UserHasRoleInProject(userId, projectId, UserRoleType.Admin))
            .ReturnsAsync(true);
        
        var filter = new AccessControlFilter(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);
        
        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };
        
        // Add a route parameter for the project ID
        actionContext.RouteData.Values["id"] = projectId.ToString();
        
        // Add the RouteParamNameAttribute to the endpoint metadata
        var metadata = new List<object> { new RouteParamNameAttribute("id") };
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(metadata), null);
        
        actionContext.HttpContext.SetEndpoint(endpoint);
        
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        
        // Act
        await filter.OnAuthorizationAsync(context);
        
        // Assert
        Assert.Null(context.Result); // No result means authorization passed
    }

    [Fact]
    public async Task OnAuthorizationAsync_MissingRouteParamNameAttribute_UsesEmptyGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        var userSessionService = new Mock<IUserSessionService>();
        var userSession = new UserSession
        {
            User = new User { Id = userId, Name = "Test User", SCIMId = "test-scim-id" },
            Collaborations = new List<Collaboration>()
        };
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync(userSession);
        
        var accessControlService = new Mock<IAccessControlService>();
        accessControlService.Setup(x => x.UserHasRoleInProject(
                userId, Guid.Empty, UserRoleType.Admin))
            .ReturnsAsync(true);
            
        var filter = new AccessControlFilter(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);
        
        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };
        
        // No RouteParamNameAttribute added to the endpoint metadata
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(), null);
        
        actionContext.HttpContext.SetEndpoint(endpoint);
        
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        
        // Act
        await filter.OnAuthorizationAsync(context);
        
        // Assert - the filter should succeed because it uses Guid.Empty
        Assert.Null(context.Result);
        
        // Verify that UserHasRoleInProject was called with the empty GUID
        accessControlService.Verify(s => s.UserHasRoleInProject(userId, Guid.Empty, UserRoleType.Admin), Times.Once);
    }
    
    [Fact]
    public async Task OnAuthorizationAsync_MissingProjectId_ThrowsArgumentNullException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        var userSessionService = new Mock<IUserSessionService>();
        var userSession = new UserSession
        {
            User = new User { Id = userId, Name = "Test User", SCIMId = "test-scim-id" },
            Collaborations = new List<Collaboration>()
        };
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync(userSession);
        
        var accessControlService = new Mock<IAccessControlService>();
        var filter = new AccessControlFilter(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);
        
        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };
        
        // No project ID in the route data
        
        // Add the RouteParamNameAttribute to the endpoint metadata
        var metadata = new List<object> { new RouteParamNameAttribute("id") };
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(metadata), null);
        
        actionContext.HttpContext.SetEndpoint(endpoint);
        
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => filter.OnAuthorizationAsync(context));
    }
    
    [Fact]
    public async Task OnAuthorizationAsync_InvalidProjectIdFormat_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        var userSessionService = new Mock<IUserSessionService>();
        var userSession = new UserSession
        {
            User = new User { Id = userId, Name = "Test User", SCIMId = "test-scim-id" },
            Collaborations = new List<Collaboration>()
        };
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync(userSession);
        
        var accessControlService = new Mock<IAccessControlService>();
        var filter = new AccessControlFilter(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);
        
        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor()
        };
        
        // Invalid project ID format in the route data
        actionContext.RouteData.Values["id"] = "not-a-guid";
        
        // Add the RouteParamNameAttribute to the endpoint metadata
        var metadata = new List<object> { new RouteParamNameAttribute("id") };
        var endpoint = new Endpoint(null, new EndpointMetadataCollection(metadata), null);
        
        actionContext.HttpContext.SetEndpoint(endpoint);
        
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => filter.OnAuthorizationAsync(context));
    }
}
