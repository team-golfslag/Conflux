using Conflux.Data;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Models;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class UserSessionServiceTests
{
    [Fact]
    public async Task GetUser_WhenFeatureFlagDisabled_ReturnsDevelopmentUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(false);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ConfluxContext(options);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockCollaborationMapper = new Mock<ICollaborationMapper>();

        var service = new UserSessionService(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object);

        // Act
        var result = await service.GetUser();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("development@sram.surf.nl", result.Email);
        Assert.Equal("Development User", result.Name);
    }

    [Fact]
    public async Task GetUser_WhenSessionNotAvailable_CallsSetUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ConfluxContext(options);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns((ISession)null!);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();

        var service = new UserSessionService(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUser());
    }

    [Fact]
    public async Task GetUser_WhenSessionHasUser_ReturnsStoredUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ConfluxContext(options);

        var mockSession = new Mock<ISession>();
        var userSession = new UserSession {
            Email = "test@example.com",
            Name = "Test User",
            SRAMId = "test-sram-id"
        };

        mockSession.Setup(s => s.IsAvailable).Returns(true);
        var serializedSession = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userSession);
        mockSession.Setup(s => s.TryGetValue("UserProfile", out serializedSession))
            .Returns(true);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();

        var service = new UserSessionService(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object);

        // Act
        var result = await service.GetUser();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.Name);
    }

    [Fact]
    public async Task UpdateUser_WhenUserFoundInDatabase_UpdatesSessionUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Database User",
            Email = "db@example.com",
            SRAMId = "sram-id-1",
            SCIMId = "scim-id-1",
        };

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ConfluxContext(options);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mockSession = new Mock<ISession>();
        var userSession = new UserSession {
            Email = "test@example.com",
            Name = "Test User",
            SRAMId = "sram-id-1"
        };

        mockSession.Setup(s => s.IsAvailable).Returns(true);
        var serializedSession = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userSession);
        mockSession.Setup(s => s.TryGetValue("UserProfile", out serializedSession))
            .Returns(true);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();

        var service = new UserSessionService(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object);

        // Act
        var result = await service.UpdateUser();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.User);
        Assert.Equal("Database User", result.User.Name);
    }

    [Fact]
    public async Task UpdateUser_WhenUserNotFoundInDatabase_ReturnsUnchangedUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ConfluxContext(options);
        // No user added to database

        var mockSession = new Mock<ISession>();
        var userSession = new UserSession {
            Email = "test@example.com",
            Name = "Test User",
            SRAMId = "sram-id-1"
        };

        mockSession.Setup(s => s.IsAvailable).Returns(true);
        var serializedSession = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userSession);
        mockSession.Setup(s => s.TryGetValue("UserProfile", out serializedSession))
            .Returns(true);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();

        var service = new UserSessionService(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object);

        // Act
        var result = await service.UpdateUser();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.User);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task CommitUser_WhenFeatureFlagDisabled_DoesNotStoreUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(false);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ConfluxContext(options);

        var mockSession = new Mock<ISession>();

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();

        var service = new UserSessionService(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object);

        var userSession = new UserSession { Email = "test@example.com" };

        // Act
        await service.CommitUser(userSession);

        // Assert
        mockSession.Verify(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task SetUser_ExtractsUserDataFromClaims()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ConfluxContext(options);

        var mockSession = new Mock<ISession>();
        mockSession.Setup(s => s.IsAvailable).Returns(true);

        var claims = new List<Claim>
        {
            new Claim("personIdentifier", "test-person-id"),
            new Claim("Name", "Test User"),
            new Claim("given_name", "Test"),
            new Claim("family_name", "User"),
            new Claim("Email", "test@example.com"),
            new Claim("Role", "urn:mace:surf.nl:sram:group:org:project1:group1")
        };

        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(principal);
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();
        mockCollaborationMapper.Setup(m => m.Map(It.IsAny<List<CollaborationDTO>>()))
            .ReturnsAsync(new List<Collaboration>());

        var service = new UserSessionService(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object);

        // Act
        var result = await service.SetUser(principal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-person-id", result.SRAMId);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("Test", result.GivenName);
        Assert.Equal("User", result.FamilyName);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public void ClearUser_RemovesUserFromSession()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ConfluxContext(options);

        var mockSession = new Mock<ISession>();
        mockSession.Setup(s => s.IsAvailable).Returns(true);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();

        var service = new UserSessionService(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object);

        // Act
        service.ClearUser();

        // Assert
        mockSession.Verify(s => s.Remove("UserProfile"), Times.Once);
    }
}