// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Controllers;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Moq;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class UserSessionControllerTests
{
    [Fact]
    public async Task LogIn_WhenUserExists_AndValidRedirect_ReturnsRedirectResult()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var mockSessionMappingService = new Mock<ISessionMappingService>();
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        var mockConfiguration = new Mock<IConfiguration>();

        UserSession userSession = new()
        {
            Email = "test@example.com",
            Name = "Test User",
        };

        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(userSession);
        mockUserSessionService.Setup(s => s.UpdateUser()).ReturnsAsync(userSession);

        string[] allowedRedirects = ["https://valid.example.com"];
        var mockConfigSection = new Mock<IConfigurationSection>();
mockConfigSection.Setup(s => s.GetChildren()).Returns(allowedRedirects.Select(r => 
{
    var section = new Mock<IConfigurationSection>();
    section.Setup(s => s.Value).Returns(r);
    return section.Object;
}));        mockConfiguration.Setup(c => c.GetSection("Authentication:SRAM:AllowedRedirectUris"))
            .Returns(mockConfigSection.Object);

        UserSessionController controller = new(
            mockUserSessionService.Object,
            mockSessionMappingService.Object,
            mockFeatureManager.Object,
            mockConfiguration.Object);

        // Act
        ActionResult result = await controller.LogIn("https://valid.example.com/path");

        // Assert
        RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://valid.example.com/path", redirectResult.Url);
    }

    [Fact]
    public async Task LogIn_WhenUserDoesNotExist_ReturnsUnauthorized()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var mockSessionMappingService = new Mock<ISessionMappingService>();
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        var mockConfiguration = new Mock<IConfiguration>();

        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync((UserSession)null!);

        string[] allowedRedirects = ["https://valid.example.com"];
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.GetChildren()).Returns(allowedRedirects.Select(r => 
        {
            var section = new Mock<IConfigurationSection>();
            section.Setup(s => s.Value).Returns(r);
            return section.Object;
        }));
        mockConfiguration.Setup(c => c.GetSection("Authentication:SRAM:AllowedRedirectUris"))
            .Returns(mockConfigSection.Object);

        UserSessionController controller = new(
            mockUserSessionService.Object,
            mockSessionMappingService.Object,
            mockFeatureManager.Object,
            mockConfiguration.Object);

        // Act
        ActionResult result = await controller.LogIn("https://valid.example.com/path");

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task LogIn_WhenRedirectUrlIsInvalid_ReturnsForbidResult()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var mockSessionMappingService = new Mock<ISessionMappingService>();
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        var mockConfiguration = new Mock<IConfiguration>();

        UserSession userSession = new()
        {
            Email = "test@example.com",
            Name = "Test User",
        };

        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(userSession);
        mockUserSessionService.Setup(s => s.UpdateUser()).ReturnsAsync(userSession);

        string[] allowedRedirects = ["https://valid.example.com"];
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.GetChildren()).Returns(allowedRedirects.Select(r => 
        {
            var section = new Mock<IConfigurationSection>();
            section.Setup(s => s.Value).Returns(r);
            return section.Object;
        }));        mockConfiguration.Setup(c => c.GetSection("Authentication:SRAM:AllowedRedirectUris"))
            .Returns(mockConfigSection.Object);

        UserSessionController controller = new(
            mockUserSessionService.Object,
            mockSessionMappingService.Object,
            mockFeatureManager.Object,
            mockConfiguration.Object);

        // Act
        ActionResult result = await controller.LogIn("https://invalid.example.com");

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task LogOut_WhenSRAMAuthenticationEnabled_ReturnsSignOutWithOpenIdAndCookie()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var mockSessionMappingService = new Mock<ISessionMappingService>();
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        var mockConfiguration = new Mock<IConfiguration>();

        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        string[] allowedRedirects = ["https://valid.example.com"];
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.GetChildren()).Returns(allowedRedirects.Select(r => 
        {
            var section = new Mock<IConfigurationSection>();
            section.Setup(s => s.Value).Returns(r);
            return section.Object;
        }));        mockConfiguration.Setup(c => c.GetSection("Authentication:SRAM:AllowedRedirectUris"))
            .Returns(mockConfigSection.Object);

        UserSessionController controller = new(
            mockUserSessionService.Object,
            mockSessionMappingService.Object,
            mockFeatureManager.Object,
            mockConfiguration.Object);

        // Need to set up controller context to test SignOut
        DefaultHttpContext httpContext = new();
        controller.ControllerContext = new()
        {
            HttpContext = httpContext,
        };

        // Act
        ActionResult result = await controller.LogOut("https://valid.example.com/path");

        // Assert
        SignOutResult signOutResult = Assert.IsType<SignOutResult>(result);
        Assert.Contains(OpenIdConnectDefaults.AuthenticationScheme, signOutResult.AuthenticationSchemes);
        Assert.Contains(CookieAuthenticationDefaults.AuthenticationScheme, signOutResult.AuthenticationSchemes);
        Assert.Equal("https://valid.example.com/path", signOutResult.Properties!.RedirectUri);
    }

    [Fact]
    public async Task LogOut_WhenSRAMAuthenticationDisabled_ReturnsSignOutWithCookieOnly()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var mockSessionMappingService = new Mock<ISessionMappingService>();
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        var mockConfiguration = new Mock<IConfiguration>();

        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(false);

        string[] allowedRedirects = ["https://valid.example.com"];
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.GetChildren()).Returns(allowedRedirects.Select(r => 
        {
            var section = new Mock<IConfigurationSection>();
            section.Setup(s => s.Value).Returns(r);
            return section.Object;
        }));        mockConfiguration.Setup(c => c.GetSection("Authentication:SRAM:AllowedRedirectUris"))
            .Returns(mockConfigSection.Object);

        UserSessionController controller = new(
            mockUserSessionService.Object,
            mockSessionMappingService.Object,
            mockFeatureManager.Object,
            mockConfiguration.Object);

        // Need to set up controller context to test SignOut
        DefaultHttpContext httpContext = new();
        controller.ControllerContext = new()
        {
            HttpContext = httpContext,
        };

        // Act
        ActionResult result = await controller.LogOut("https://valid.example.com/path");

        // Assert
        SignOutResult signOutResult = Assert.IsType<SignOutResult>(result);
        Assert.DoesNotContain(OpenIdConnectDefaults.AuthenticationScheme, signOutResult.AuthenticationSchemes);
        Assert.Contains(CookieAuthenticationDefaults.AuthenticationScheme, signOutResult.AuthenticationSchemes);
        Assert.Equal("https://valid.example.com/path", signOutResult.Properties!.RedirectUri);
    }

    [Fact]
    public async Task LogOut_WhenRedirectUrlIsInvalid_UsesDefaultRedirect()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var mockSessionMappingService = new Mock<ISessionMappingService>();
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        var mockConfiguration = new Mock<IConfiguration>();

        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        string[] allowedRedirects = ["https://valid.example.com"];
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.GetChildren()).Returns(allowedRedirects.Select(r => 
        {
            var section = new Mock<IConfigurationSection>();
            section.Setup(s => s.Value).Returns(r);
            return section.Object;
        }));        mockConfiguration.Setup(c => c.GetSection("Authentication:SRAM:AllowedRedirectUris"))
            .Returns(mockConfigSection.Object);

        UserSessionController controller = new(
            mockUserSessionService.Object,
            mockSessionMappingService.Object,
            mockFeatureManager.Object,
            mockConfiguration.Object);

        // Need to set up controller context to test SignOut
        DefaultHttpContext httpContext = new();
        controller.ControllerContext = new()
        {
            HttpContext = httpContext,
        };

        // Act
        ActionResult result = await controller.LogOut("https://invalid.example.com");

        // Assert
        SignOutResult signOutResult = Assert.IsType<SignOutResult>(result);
        Assert.Equal("/", signOutResult.Properties!.RedirectUri);
    }

   [Fact]
public async Task UserSession_WhenUserExists_ReturnsUserSession()
{
    // Arrange
    var mockUserSessionService = new Mock<IUserSessionService>();
    var mockSessionMappingService = new Mock<ISessionMappingService>();
    var mockFeatureManager = new Mock<IVariantFeatureManager>();
    var mockConfiguration = new Mock<IConfiguration>();

    UserSession userSession = new()
    {
        Email = "test@example.com",
        Name = "Test User",
    };

    mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(userSession);

    // Set up configuration
    string[] allowedRedirects = ["https://valid.example.com"];
    var mockConfigSection = new Mock<IConfigurationSection>();
    mockConfigSection.Setup(s => s.GetChildren()).Returns(allowedRedirects.Select(r =>
    {
        var section = new Mock<IConfigurationSection>();
        section.Setup(s => s.Value).Returns(r);
        return section.Object;
    }));
    mockConfiguration.Setup(c => c.GetSection("Authentication:SRAM:AllowedRedirectUris"))
        .Returns(mockConfigSection.Object);

    UserSessionController controller = new(
        mockUserSessionService.Object,
        mockSessionMappingService.Object,
        mockFeatureManager.Object,
        mockConfiguration.Object);

    // Act
    var result = await controller.UserSession();

    // Assert
    var actionResult = Assert.IsType<ActionResult<UserSession>>(result);
    UserSession returnedSession = Assert.IsType<UserSession>(actionResult.Value);
    Assert.Equal("test@example.com", returnedSession.Email);
    Assert.Equal("Test User", returnedSession.Name);
}

[Fact]
public async Task UserSession_WhenUserDoesNotExist_ThrowsInvalidOperationException()
{
    // Arrange
    var mockUserSessionService = new Mock<IUserSessionService>();
    var mockSessionMappingService = new Mock<ISessionMappingService>();
    var mockFeatureManager = new Mock<IVariantFeatureManager>();
    var mockConfiguration = new Mock<IConfiguration>();

    mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync((UserSession)null!);

    // Set up configuration
    string[] allowedRedirects = ["https://valid.example.com"];
    var mockConfigSection = new Mock<IConfigurationSection>();
    mockConfigSection.Setup(s => s.GetChildren()).Returns(allowedRedirects.Select(r =>
    {
        var section = new Mock<IConfigurationSection>();
        section.Setup(s => s.Value).Returns(r);
        return section.Object;
    }));
    mockConfiguration.Setup(c => c.GetSection("Authentication:SRAM:AllowedRedirectUris"))
        .Returns(mockConfigSection.Object);

    UserSessionController controller = new(
        mockUserSessionService.Object,
        mockSessionMappingService.Object,
        mockFeatureManager.Object,
        mockConfiguration.Object);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => controller.UserSession());
}
}
