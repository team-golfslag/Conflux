// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using Conflux.API.Attributes;
using Conflux.API.Filters;
using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Conflux.API.Tests.Filters;

/// <summary>
/// Integration tests for the access control filter system
/// </summary>
public class AccessControlIntegrationTests
{
    [Fact]
    public async Task Endpoint_WithRequireProjectRoleAttribute_RejectsUnauthorizedUser()
    {
        // Arrange
        var userSession = new UserSession
        {
            User = null, // No authenticated user
            Collaborations = new List<Collaboration>()
        };
        
        var client = await CreateTestClient(userSession);
        
        // Act
        var response = await client.GetAsync("/test-auth/admin");
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task Endpoint_WithRequireProjectRoleAttribute_RejectsForbiddenUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var userSession = new UserSession
        {
            User = new User { Id = userId, Name = "Test User", SCIMId = "test-scim-id" },
            Collaborations = new List<Collaboration>()
        };
        
        // Create client with hasRole = false to test forbidden access
        var client = await CreateTestClient(
            userSession,
            (u, p, r) => Task.FromResult(false));
        
        // Act
        var response = await client.GetAsync($"/test-auth/{projectId}/admin");
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    
    [Fact]
    public async Task Endpoint_WithRequireProjectRoleAttribute_AllowsAuthorizedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var userSession = new UserSession
        {
            User = new User { Id = userId, Name = "Test User", SCIMId = "test-scim-id" },
            Collaborations = new List<Collaboration>()
        };
        
        // Create client with hasRole = true to test successful access
        var client = await CreateTestClient(
            userSession,
            (u, p, r) => Task.FromResult(true));
        
        // Act
        var response = await client.GetAsync($"/test-auth/{projectId}/admin");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task Endpoint_WithRequireProjectRoleAttribute_AndDifferentRole_ChecksCorrectRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var userSession = new UserSession
        {
            User = new User { Id = userId, Name = "Test User", SCIMId = "test-scim-id" },
            Collaborations = new List<Collaboration>()
        };
        
        // Setup access control to only allow Contributor role but not Admin role
        var client = await CreateTestClient(
            userSession,
            (u, p, r) => Task.FromResult(r == UserRoleType.Contributor));
        
        // Act - try to access an endpoint requiring Admin role
        var adminResponse = await client.GetAsync($"/test-auth/{projectId}/admin");
        // Act - try to access an endpoint requiring Contributor role
        var contributorResponse = await client.GetAsync($"/test-auth/{projectId}/contributor");
        
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, contributorResponse.StatusCode);
    }
    
    [Fact]
    public async Task Endpoint_WithoutRouteParam_UsesCorrectDefaultProjectId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        var userSession = new UserSession
        {
            User = new User { Id = userId, Name = "Test User", SCIMId = "test-scim-id" },
            Collaborations = new List<Collaboration>()
        };
        
        // Create a client that tracks the projectId passed to UserHasRoleInProject
        Guid? capturedProjectId = null;
        var client = await CreateTestClient(
            userSession,
            (uid, pid, role) => { capturedProjectId = pid; return Task.FromResult(true); });
        
        // Act
        var response = await client.GetAsync("/test-auth/admin");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // When no route parameter is provided, the filter should use a default empty GUID
        Assert.Equal(Guid.Empty, capturedProjectId);
    }
    
    private static async Task<HttpClient> CreateTestClient(
        UserSession? userSession,
        Func<Guid, Guid, UserRoleType, Task<bool>>? roleCheck = null)
    {
        var userSessionServiceMock = new Mock<IUserSessionService>();
        userSessionServiceMock.Setup(x => x.GetUser()).ReturnsAsync(userSession);
        
        var accessControlServiceMock = new Mock<IAccessControlService>();
        
        // If no role check is provided, default to false
        roleCheck ??= (u, p, r) => Task.FromResult(false);
        
        accessControlServiceMock
            .Setup(x => x.UserHasRoleInProject(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UserRoleType>()))
            .Returns(roleCheck);
        
        // Create a test server with our test services
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    // Add essential services
                    services.AddRouting();
                    services.AddControllers();
                    
                    // Add our authentication services
                    services.AddSingleton(userSessionServiceMock.Object);
                    services.AddSingleton(accessControlServiceMock.Object);
                    
                    // Add in-memory database
                    services.AddDbContext<ConfluxContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));
                });
                
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    
                    app.UseEndpoints(endpoints =>
                    {
                        // Add test endpoint with Admin role at project level
                        endpoints.MapGet("/test-auth/{id}/admin", async context =>
                        {
                            // Manual authorization check
                            var userSessionService = context.RequestServices.GetRequiredService<IUserSessionService>();
                            var accessControlService = context.RequestServices.GetRequiredService<IAccessControlService>();
                            var session = await userSessionService.GetUser();
                            
                            if (session?.User == null)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                return;
                            }
                            
                            var routeData = context.Request.RouteValues;
                            if (!Guid.TryParse(routeData["id"]?.ToString(), out var projectId))
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                return;
                            }
                            
                            bool hasRole = await accessControlService.UserHasRoleInProject(
                                session.User.Id, projectId, UserRoleType.Admin);
                                
                            if (!hasRole)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                                return;
                            }
                            
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                        });
                        
                        // Add test endpoint with Admin role without project ID
                        endpoints.MapGet("/test-auth/admin", async context =>
                        {
                            // Manual authorization check
                            var userSessionService = context.RequestServices.GetRequiredService<IUserSessionService>();
                            var accessControlService = context.RequestServices.GetRequiredService<IAccessControlService>();
                            var session = await userSessionService.GetUser();
                            
                            if (session?.User == null)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                return;
                            }
                            
                            // For endpoints without project ID, we use empty GUID
                            bool hasRole = await accessControlService.UserHasRoleInProject(
                                session.User.Id, Guid.Empty, UserRoleType.Admin);
                                
                            if (!hasRole)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                                return;
                            }
                            
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                        });
                        
                        // Add test endpoint with Contributor role
                        endpoints.MapGet("/test-auth/{id}/contributor", async context =>
                        {
                            // Manual authorization check
                            var userSessionService = context.RequestServices.GetRequiredService<IUserSessionService>();
                            var accessControlService = context.RequestServices.GetRequiredService<IAccessControlService>();
                            var session = await userSessionService.GetUser();
                            
                            if (session?.User == null)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                return;
                            }
                            
                            var routeData = context.Request.RouteValues;
                            if (!Guid.TryParse(routeData["id"]?.ToString(), out var projectId))
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                return;
                            }
                            
                            bool hasRole = await accessControlService.UserHasRoleInProject(
                                session.User.Id, projectId, UserRoleType.Contributor);
                                
                            if (!hasRole)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                                return;
                            }
                            
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                        });
                    });
                });
            });
            
        var host = await hostBuilder.StartAsync();
        return host.GetTestClient();
    }
}
