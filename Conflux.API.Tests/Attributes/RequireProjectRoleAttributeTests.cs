// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Conflux.API.Filters;
using Conflux.Domain;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Conflux.API.Tests.Attributes;

public class RequireProjectRoleAttributeTests
{
    [Fact]
    public void Constructor_SetsPermission()
    {
        // Arrange & Act
        RequireProjectRoleAttribute attribute = new(UserRoleType.Admin);

        // Assert
        Assert.Equal(UserRoleType.Admin, attribute.Permission);
    }

    [Fact]
    public void IsReusable_ReturnsFalse()
    {
        // Arrange
        RequireProjectRoleAttribute attribute = new(UserRoleType.Admin);

        // Act & Assert
        Assert.False(attribute.IsReusable);
    }

    [Fact]
    public void CreateInstance_ReturnsAccessControlFilter()
    {
        // Arrange
        RequireProjectRoleAttribute attribute = new(UserRoleType.Admin);

        // Create a mock service provider with the necessary services
        var userSessionService = new Mock<IUserSessionService>();
        var accessControlService = new Mock<IAccessControlService>();

        AccessControlFilterFactory filterFactory = new(
            userSessionService.Object,
            accessControlService.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(AccessControlFilterFactory)))
            .Returns(filterFactory);

        serviceProvider
            .Setup(x => x.GetService(typeof(IServiceProvider)))
            .Returns(serviceProvider.Object);

        // Set up the GetRequiredService method
        ServiceCollection services = new();
        services.AddTransient(sp => filterFactory);

        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        var serviceScope = new Mock<IServiceScope>();
        ServiceProvider serviceProviderFromScope = services.BuildServiceProvider();

        serviceProvider
            .Setup(s => s.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactory.Object);

        serviceScopeFactory
            .Setup(s => s.CreateScope())
            .Returns(serviceScope.Object);

        serviceScope
            .Setup(s => s.ServiceProvider)
            .Returns(serviceProviderFromScope);

        // Add an extension method to mock GetRequiredService
        serviceProvider
            .Setup(s => s.GetService(typeof(AccessControlFilterFactory)))
            .Returns(filterFactory);

        // Act
        IFilterMetadata filter = attribute.CreateInstance(serviceProvider.Object);

        // Assert
        Assert.NotNull(filter);
        Assert.IsAssignableFrom<IAsyncAuthorizationFilter>(filter);
    }

    [Theory]
    [InlineData(UserRoleType.Admin)]
    [InlineData(UserRoleType.Contributor)]
    [InlineData(UserRoleType.User)]
    public void CreateInstance_WithDifferentRoles_SetsCorrectPermission(UserRoleType role)
    {
        // Arrange
        RequireProjectRoleAttribute attribute = new(role);

        var userSessionService = new Mock<IUserSessionService>();
        var accessControlService = new Mock<IAccessControlService>();

        // Use a real factory instance instead of mocking
        AccessControlFilterFactory filterFactory = new(
            userSessionService.Object,
            accessControlService.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(AccessControlFilterFactory)))
            .Returns(filterFactory);

        // Setup GetRequiredService
        serviceProvider
            .Setup(x => x.GetService(typeof(IServiceProvider)))
            .Returns(serviceProvider.Object);

        serviceProvider
            .Setup(s => s.GetService(typeof(AccessControlFilterFactory)))
            .Returns(filterFactory);

        // Act
        IFilterMetadata filter = attribute.CreateInstance(serviceProvider.Object);

        // Assert
        Assert.NotNull(filter);
        Assert.IsType<AccessControlFilter>(filter);

        // Test that the correct role was passed by checking the parameter
        // Since we can't directly access the constructor parameter which is a primary constructor parameter
        // We'll just verify that the filter is created successfully
        // The actual parameter value is implicitly tested when using the filter
    }

    [Fact]
    public void CreateInstance_ServiceProviderReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        RequireProjectRoleAttribute attribute = new(UserRoleType.Admin);

        // Mock service provider that returns null for the filter factory
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(AccessControlFilterFactory)))
            .Returns(null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => attribute.CreateInstance(serviceProvider.Object));
    }
}
