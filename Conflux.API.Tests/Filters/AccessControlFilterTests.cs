// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.API.Filters;
using Conflux.Domain;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;
using Xunit;

namespace Conflux.API.Tests.Filters;

public class AccessControlFilterTests
{
    private static User CreateUserWithPerson(Guid userId, string name, string scimId, string? orcid = null)
    {
        var personId = Guid.CreateVersion7();
        
        // Create the person first
        var person = new Person
        {
            Id = personId,
            Name = name,
            ORCiD = orcid,
            User = null
        };
        
        // Then create the user with a reference to the person
        var user = new User
        {
            Id = userId,
            SCIMId = scimId,
            PersonId = personId,
            Person = person
        };
        
        // Set the bidirectional reference
        person.User = user;
        return user;
    }

    [Fact]
    public async Task OnAuthorizationAsync_UserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var userSessionService = new Mock<IUserSessionService>();
        userSessionService.Setup(x => x.GetUser()).Throws(new UserNotAuthenticatedException());

        var accessControlService = new Mock<IAccessControlService>();
        AccessControlFilter filter = new(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);

        ActionContext actionContext = new()
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new(),
            ActionDescriptor = new(),
        };
        AuthorizationFilterContext context = new(actionContext, new List<IFilterMetadata>());

        await Assert.ThrowsAsync<UserNotAuthenticatedException>(() => filter.OnAuthorizationAsync(context));
    }

    [Fact]
    public async Task OnAuthorizationAsync_UserHasNoRole_ThrowsUnauthorized()
    {
        // Arrange
        Guid userId = Guid.CreateVersion7();
        Guid projectId = Guid.CreateVersion7();

        var userSessionService = new Mock<IUserSessionService>();
        User user = CreateUserWithPerson(userId, "Test User", "test-scim-id");
        UserSession userSession = new()
        {
            UserId = userId,
            Collaborations = new(),
        };
        userSessionService.Setup(x => x.GetSession()).ReturnsAsync(userSession);
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync(user);


        var accessControlService = new Mock<IAccessControlService>();
        accessControlService.Setup(x => x.UserHasRoleInProject(userId, projectId, UserRoleType.Admin))
            .ReturnsAsync(false);

        AccessControlFilter filter = new(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);

        ActionContext actionContext = new()
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new(),
            ActionDescriptor = new(),
        };

        // Add a route parameter for the project ID
        actionContext.RouteData.Values["id"] = projectId.ToString();

        // Add the RouteParamNameAttribute to the endpoint metadata
        var metadata = new List<object>
        {
            new RouteParamNameAttribute("id"),
        };
        Endpoint endpoint = new(null, new(metadata), null);

        actionContext.HttpContext.SetEndpoint(endpoint);

        AuthorizationFilterContext context = new(actionContext, new List<IFilterMetadata>());

        // Act
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => filter.OnAuthorizationAsync(context));
    }

    [Fact]
    public async Task OnAuthorizationAsync_UserHasRole_ReturnsNull()
    {
        // Arrange
        Guid userId = Guid.CreateVersion7();
        Guid projectId = Guid.CreateVersion7();

        var userSessionService = new Mock<IUserSessionService>();
        User user = CreateUserWithPerson(userId, "Test User", "test-scim-id");
        UserSession userSession = new()
        {
            UserId = userId,
            Collaborations = new(),
        };
        userSessionService.Setup(x => x.GetSession()).ReturnsAsync(userSession);
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync(user);


        var accessControlService = new Mock<IAccessControlService>();
        accessControlService.Setup(x => x.UserHasRoleInProject(userId, projectId, UserRoleType.Admin))
            .ReturnsAsync(true);

        AccessControlFilter filter = new(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);

        ActionContext actionContext = new()
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new(),
            ActionDescriptor = new(),
        };

        // Add a route parameter for the project ID
        actionContext.RouteData.Values["id"] = projectId.ToString();

        // Add the RouteParamNameAttribute to the endpoint metadata
        var metadata = new List<object>
        {
            new RouteParamNameAttribute("id"),
        };
        Endpoint endpoint = new(null, new(metadata), null);

        actionContext.HttpContext.SetEndpoint(endpoint);

        AuthorizationFilterContext context = new(actionContext, new List<IFilterMetadata>());

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        Assert.Null(context.Result); // No result means authorization passed
    }

    [Fact]
    public async Task OnAuthorizationAsync_MissingRouteParamNameAttribute_UsesEmptyGuid()
    {
        // Arrange
        Guid userId = Guid.CreateVersion7();

        var userSessionService = new Mock<IUserSessionService>();
        User user = CreateUserWithPerson(userId, "Test User", "test-scim-id");
        UserSession userSession = new()
        {
            UserId = userId,
            Collaborations = new(),
        };
        userSessionService.Setup(x => x.GetSession()).ReturnsAsync(userSession);
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync(user);

        var accessControlService = new Mock<IAccessControlService>();
        accessControlService.Setup(x => x.UserHasRoleInProject(
                userId, Guid.Empty, UserRoleType.Admin))
            .ReturnsAsync(true);

        AccessControlFilter filter = new(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);

        ActionContext actionContext = new()
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new(),
            ActionDescriptor = new(),
        };

        // No RouteParamNameAttribute added to the endpoint metadata
        Endpoint endpoint = new(null, new(), null);

        actionContext.HttpContext.SetEndpoint(endpoint);

        AuthorizationFilterContext context = new(actionContext, new List<IFilterMetadata>());

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
        Guid userId = Guid.CreateVersion7();

        var userSessionService = new Mock<IUserSessionService>();
        User user = CreateUserWithPerson(userId, "Test User", "test-scim-id");
        UserSession userSession = new()
        {
            UserId = userId,
            Collaborations = new(),
        };
        userSessionService.Setup(x => x.GetSession()).ReturnsAsync(userSession);
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync(user);

        var accessControlService = new Mock<IAccessControlService>();
        AccessControlFilter filter = new(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);

        ActionContext actionContext = new()
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new(),
            ActionDescriptor = new(),
        };

        // No project ID in the route data

        // Add the RouteParamNameAttribute to the endpoint metadata
        var metadata = new List<object>
        {
            new RouteParamNameAttribute("id"),
        };
        Endpoint endpoint = new(null, new(metadata), null);

        actionContext.HttpContext.SetEndpoint(endpoint);

        AuthorizationFilterContext context = new(actionContext, new List<IFilterMetadata>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => filter.OnAuthorizationAsync(context));
    }

    [Fact]
    public async Task OnAuthorizationAsync_InvalidProjectIdFormat_ThrowsArgumentException()
    {
        // Arrange
        Guid userId = Guid.CreateVersion7();

        var userSessionService = new Mock<IUserSessionService>();
        User user = CreateUserWithPerson(userId, "Test User", "test-scim-id");
        UserSession userSession = new()
        {
            UserId = userId,
            Collaborations = new(),
        };
        userSessionService.Setup(x => x.GetSession()).ReturnsAsync(userSession);
        userSessionService.Setup(x => x.GetUser()).ReturnsAsync(user);

        var accessControlService = new Mock<IAccessControlService>();
        AccessControlFilter filter = new(userSessionService.Object, accessControlService.Object, UserRoleType.Admin);

        ActionContext actionContext = new()
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new(),
            ActionDescriptor = new(),
        };

        // Invalid project ID format in the route data
        actionContext.RouteData.Values["id"] = "not-a-guid";

        // Add the RouteParamNameAttribute to the endpoint metadata
        var metadata = new List<object>
        {
            new RouteParamNameAttribute("id"),
        };
        Endpoint endpoint = new(null, new(metadata), null);

        actionContext.HttpContext.SetEndpoint(endpoint);

        AuthorizationFilterContext context = new(actionContext, new List<IFilterMetadata>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => filter.OnAuthorizationAsync(context));
    }
}
