// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class AccessControlServiceTests
{
    private ConfluxContext CreateContext(string dbName)
    {
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(dbName)
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        var context = new ConfluxContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task UserHasRoleInProject_UserWithRoleExists_ReturnsTrue()
    {
        // Arrange
        string dbName = $"UserHasRoleTest_{Guid.NewGuid()}";
        var context = CreateContext(dbName);
        
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        UserRoleType roleType = UserRoleType.Admin;
        
        User user = new User
        {
            Id = userId,
            Roles =
            [
                new()
                {
                    ProjectId = projectId,
                    Type = roleType,
                    Id = Guid.NewGuid(),
                    Urn = "test-urn",
                    SCIMId = "test-scim-id",
                },
            ],
            SCIMId = "user-scim-id",
            Name = "Test User",
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        AccessControlService service = new AccessControlService(context);
        
        // Act
        bool result = await service.UserHasRoleInProject(userId, projectId, roleType);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_UserWithDifferentRoleExists_ReturnsFalse()
    {
        // Arrange
        string dbName = $"UserHasDifferentRoleTest_{Guid.NewGuid()}";
        var context = CreateContext(dbName);
        
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        
        User user = new User
        {
            Id = userId,
            Roles =
            [
                new()
                {
                    ProjectId = projectId,
                    Type = UserRoleType.User,
                    Id = Guid.NewGuid(),
                    Urn = "test-urn",
                    SCIMId = "test-scim-id",
                },
            ],
            SCIMId = "user-scim-id",
            Name = "Test User",
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        AccessControlService service = new AccessControlService(context);
        
        // Act
        bool result = await service.UserHasRoleInProject(userId, projectId, UserRoleType.Admin);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_UserWithRoleInDifferentProject_ReturnsFalse()
    {
        // Arrange
        string dbName = $"UserHasRoleInDifferentProjectTest_{Guid.NewGuid()}";
        var context = CreateContext(dbName);
        
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid differentProjectId = Guid.NewGuid();
        
        User user = new User
        {
            Id = userId,
            Roles =
            [
                new()
                {
                    ProjectId = differentProjectId,
                    Type = UserRoleType.Admin,
                    Id = Guid.NewGuid(),
                    Urn = "test-urn",
                    SCIMId = "test-scim-id",
                },
            ],
            SCIMId = "user-scim-id",
            Name = "Test User",
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        AccessControlService service = new AccessControlService(context);
        
        // Act
        bool result = await service.UserHasRoleInProject(userId, projectId, UserRoleType.Admin);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_UserWithNoRoles_ReturnsFalse()
    {
        // Arrange
        string dbName = $"UserHasNoRolesTest_{Guid.NewGuid()}";
        var context = CreateContext(dbName);
        
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        
        User user = new User
        {
            Id = userId,
            Roles = [],
            SCIMId = "user-scim-id",
            Name = "Test User",
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        AccessControlService service = new AccessControlService(context);
        
        // Act
        bool result = await service.UserHasRoleInProject(userId, projectId, UserRoleType.Admin);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        string dbName = $"UserDoesNotExistTest_{Guid.NewGuid()}";
        var context = CreateContext(dbName);
        
        Guid userId = Guid.NewGuid(); // User that doesn't exist
        Guid projectId = Guid.NewGuid();
        
        AccessControlService service = new AccessControlService(context);
        
        // Act
        bool result = await service.UserHasRoleInProject(userId, projectId, UserRoleType.Admin);
        
        // Assert
        Assert.False(result);
    }
}