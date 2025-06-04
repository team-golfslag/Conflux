// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using System.Text.Json;
using Conflux.Data;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Conflux.Integrations.SRAM;
using Conflux.Integrations.SRAM.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Moq;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class UserSessionServiceTests
{
    [Fact]
    public async Task GetUser_WhenFeatureFlagDisabled_ReturnsDevelopmentUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(false);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        ConfluxContext context = new(options);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockCollaborationMapper = new Mock<ICollaborationMapper>();
        var configurationMock = new Mock<IConfiguration>();
        
        UserSessionService service = new(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object, configurationMock.Object);

        // Act
        UserSession? result = await service.GetUser();

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
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        ConfluxContext context = new(options);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns((ISession)null!);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();
        var configurationMock = new Mock<IConfiguration>();

        UserSessionService service = new(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object, configurationMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotAuthenticatedException>(() => service.GetUser());
    }

    [Fact]
    public async Task GetUser_WhenSessionHasUser_ReturnsStoredUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        ConfluxContext context = new(options);

        var mockSession = new Mock<ISession>();
        UserSession userSession = new()
        {
            Email = "test@example.com",
            Name = "Test User",
            SRAMId = "test-sram-id",
        };

        mockSession.Setup(s => s.IsAvailable).Returns(true);
        byte[]? serializedSession = JsonSerializer.SerializeToUtf8Bytes(userSession);
        mockSession.Setup(s => s.TryGetValue("UserProfile", out serializedSession))
            .Returns(true);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();
        var configurationMock = new Mock<IConfiguration>();

        UserSessionService service = new(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object, configurationMock.Object);

        // Act
        UserSession? result = await service.GetUser();

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
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        // Create the person first with user's personal info
        Person person = new()
        {
            Id = Guid.NewGuid(),
            Name = "Database User",
            Email = "db@example.com",
        };
        
        // Then create the user with reference to the person
        User user = new()
        {
            Id = Guid.NewGuid(),
            SRAMId = "sram-id-1",
            SCIMId = "scim-id-1",
            PersonId = person.Id,
            Person = person
        };
        
        // Set bidirectional reference
        person.User = user;

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        ConfluxContext context = new(options);
        // Add both person and user
        context.People.Add(person);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var mockSession = new Mock<ISession>();
        UserSession userSession = new()
        {
            Email = "test@example.com",
            Name = "Test User",
            SRAMId = "sram-id-1",
        };

        mockSession.Setup(s => s.IsAvailable).Returns(true);
        byte[]? serializedSession = JsonSerializer.SerializeToUtf8Bytes(userSession);
        mockSession.Setup(s => s.TryGetValue("UserProfile", out serializedSession))
            .Returns(true);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();
        var configurationMock = new Mock<IConfiguration>();

        UserSessionService service = new(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object, configurationMock.Object);

        // Act
        UserSession? result = await service.UpdateUser();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.User);
        Assert.Equal("Database User", result.User.Person?.Name);
    }

    [Fact]
    public async Task UpdateUser_WhenUserNotFoundInDatabase_ReturnsUnchangedUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        ConfluxContext context = new(options);
        // No user added to database

        var mockSession = new Mock<ISession>();
        UserSession userSession = new()
        {
            Email = "test@example.com",
            Name = "Test User",
            SRAMId = "sram-id-1",
        };

        mockSession.Setup(s => s.IsAvailable).Returns(true);
        byte[]? serializedSession = JsonSerializer.SerializeToUtf8Bytes(userSession);
        mockSession.Setup(s => s.TryGetValue("UserProfile", out serializedSession))
            .Returns(true);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();
        var configurationMock = new Mock<IConfiguration>();

        UserSessionService service = new(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object, configurationMock.Object);

        // Act
        UserSession? result = await service.UpdateUser();

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
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(false);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        ConfluxContext context = new(options);

        var mockSession = new Mock<ISession>();

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();
        var configurationMock = new Mock<IConfiguration>();

        UserSessionService service = new(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object, configurationMock.Object);

        UserSession userSession = new()
        {
            Email = "test@example.com",
        };

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
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        ConfluxContext context = new(options);

        var mockSession = new Mock<ISession>();
        mockSession.Setup(s => s.IsAvailable).Returns(true);

        var claims = new List<Claim>
        {
            new("personIdentifier", "test-person-id"),
            new("Name", "Test User"),
            new("given_name", "Test"),
            new("family_name", "User"),
            new("Email", "test@example.com"),
            new("Role", "urn:mace:surf.nl:sram:group:org:project1:group1"),
        };

        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(principal);
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();
        mockCollaborationMapper.Setup(m => m.Map(It.IsAny<List<CollaborationDTO>>()))
            .ReturnsAsync([]);
        var configurationMock = new Mock<IConfiguration>();

        UserSessionService service = new(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object, configurationMock.Object);

        // Act
        UserSession? result = await service.SetUser(principal);

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
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        ConfluxContext context = new(options);

        var mockSession = new Mock<ISession>();
        mockSession.Setup(s => s.IsAvailable).Returns(true);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        var mockCollaborationMapper = new Mock<ICollaborationMapper>();
        var configurationMock = new Mock<IConfiguration>();

        UserSessionService service = new(context, mockHttpContextAccessor.Object,
            mockCollaborationMapper.Object, mockFeatureManager.Object, configurationMock.Object);

        // Act
        service.ClearUser();

        // Assert
        mockSession.Verify(s => s.Remove("UserProfile"), Times.Once);
    }
}

