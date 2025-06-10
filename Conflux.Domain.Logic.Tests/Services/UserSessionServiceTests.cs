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

public class UserSessionServiceTests : IDisposable
{
    private readonly ConfluxContext _context;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ICollaborationMapper> _mockCollaborationMapper;
    private readonly Mock<IVariantFeatureManager> _mockFeatureManager;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ISession> _mockSession;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly UserSessionService _service;

    public UserSessionServiceTests()
    {
        // Database context setup
        DbContextOptions<ConfluxContext> options =
            new DbContextOptionsBuilder<ConfluxContext>()
                .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
                .Options;
        _context = new ConfluxContext(options);

        // Common Mocks
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockCollaborationMapper = new Mock<ICollaborationMapper>();
        _mockFeatureManager = new Mock<IVariantFeatureManager>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockSession = new Mock<ISession>();
        _mockHttpContext = new Mock<HttpContext>();

        // Common Mock setups
        _mockHttpContext.Setup(c => c.Session).Returns(_mockSession.Object);
        _mockHttpContextAccessor.Setup(a => a.HttpContext)
            .Returns(_mockHttpContext.Object);
        _mockSession.Setup(s => s.IsAvailable).Returns(true);
        
        // Setup the SuperAdminEmails configuration section
        var emails = new List<string>();
        var mockEmailSections = emails.Select(email =>
        {
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(s => s.Value).Returns(email);
            return mockSection.Object;
        }).ToList();

        var mockSuperAdminSection = new Mock<IConfigurationSection>();
        mockSuperAdminSection.Setup(s => s.GetChildren())
            .Returns(mockEmailSections);

        _mockConfiguration.Setup(c => c.GetSection("SuperAdminEmails"))
            .Returns(mockSuperAdminSection.Object);
        

        // Service under test
        _service = new UserSessionService(
            _context,
            _mockHttpContextAccessor.Object,
            _mockCollaborationMapper.Object,
            _mockFeatureManager.Object,
            _mockConfiguration.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    private void SetupFeatureFlag(bool isEnabled)
    {
        _mockFeatureManager
            .Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(isEnabled);
    }

   

    private async Task<User> CreateAndAddUser(
        string name,
        string email,
        string sramId,
        PermissionLevel permissionLevel = PermissionLevel.User
    )
    {
        var person = new Person { Id = Guid.CreateVersion7(), Name = name, Email = email, };

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            SRAMId = sramId,
            SCIMId = $"scim-id-{sramId}",
            PersonId = person.Id,
            Person = person,
            PermissionLevel = permissionLevel
        };

        person.User = user;
        _context.People.Add(person);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private void SetupSessionWithUser(UserSession userSession)
    {
        byte[]? serializedSession = JsonSerializer.SerializeToUtf8Bytes(userSession);
        _mockSession.Setup(s => s.TryGetValue("UserProfile", out serializedSession))
            .Returns(true);
    }

    private ClaimsPrincipal CreateClaimsPrincipal(
        string sramId,
        string name,
        string email,
        string givenName = "Test",
        string familyName = "User"
    )
    {
        var claims = new List<Claim>
        {
            new("personIdentifier", sramId),
            new("Name", name),
            new("given_name", givenName),
            new("family_name", familyName),
            new("Email", email),
            new("Role", "urn:mace:surf.nl:sram:group:org:project1:group1"),
        };
        var identity = new ClaimsIdentity(claims);
        return new ClaimsPrincipal(identity);
    }

    #endregion

    [Fact]
    public async Task GetUser_WhenFeatureFlagDisabled_ReturnsDevelopmentUser()
    {
        // Arrange
        SetupFeatureFlag(false);

        // Act
        UserSession? result = await _service.GetUser();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("development@sram.surf.nl", result.Email);
        Assert.Equal("Development User", result.Name);
    }

    [Fact]
    public async Task GetUser_WhenSessionNotAvailable_ThrowsException()
    {
        // Arrange
        SetupFeatureFlag(true);
        _mockHttpContext.Setup(c => c.Session).Returns((ISession)null!);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotAuthenticatedException>(
            () => _service.GetUser()
        );
    }

    [Fact]
    public async Task GetUser_WhenSessionHasUser_ReturnsStoredUser()
    {
        // Arrange
        SetupFeatureFlag(true);
        var userSession = new UserSession
        {
            Email = "test@example.com",
            Name = "Test User",
            SRAMId = "test-sram-id",
        };
        SetupSessionWithUser(userSession);

        // Act
        UserSession? result = await _service.GetUser();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.Name);
    }

    [Fact]
    public async Task UpdateUser_WhenUserFoundInDatabase_UpdatesSessionUser()
    {
        // Arrange
        SetupFeatureFlag(true);
        User dbUser = await CreateAndAddUser(
            "Database User",
            "db@example.com",
            "sram-id-1"
        );
        var userSession = new UserSession
        {
            Email = "test@example.com",
            Name = "Test User",
            SRAMId = "sram-id-1",
        };
        SetupSessionWithUser(userSession);

        // Act
        UserSession? result = await _service.UpdateUser();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.User);
        Assert.Equal(dbUser.Person.Name, result.User.Person?.Name);
    }

    [Fact]
    public async Task UpdateUser_WhenUserNotFoundInDatabase_ReturnsUnchangedUser()
    {
        // Arrange
        SetupFeatureFlag(true);
        var userSession = new UserSession
        {
            Email = "test@example.com",
            Name = "Test User",
            SRAMId = "sram-id-1",
        };
        SetupSessionWithUser(userSession);

        // Act
        UserSession? result = await _service.UpdateUser();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.User);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task CommitUser_WhenFeatureFlagDisabled_DoesNotStoreUser()
    {
        // Arrange
        SetupFeatureFlag(false);
        var userSession = new UserSession { Email = "test@example.com", };

        // Act
        await _service.CommitUser(userSession);

        // Assert
        _mockSession.Verify(
            s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SetUser_ExtractsUserDataFromClaims()
    {
        // Arrange
        SetupFeatureFlag(true);
        ClaimsPrincipal principal = CreateClaimsPrincipal(
            "test-person-id",
            "Test User",
            "test@example.com"
        );
        _mockHttpContext.Setup(c => c.User).Returns(principal);
        _mockCollaborationMapper
            .Setup(m => m.Map(It.IsAny<List<CollaborationDTO>>()))
            .ReturnsAsync(new List<Collaboration>());

        // Act
        UserSession? result = await _service.SetUser(principal);

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
        // Act
        _service.ClearUser();

        // Assert
        _mockSession.Verify(s => s.Remove("UserProfile"), Times.Once);
    }
    
    [Fact]
    public async Task GetUser_WhenUserEmailInSuperAdminEmails_PromotesToSuperAdmin()
    {
        // Arrange
        // we have to setup the entire user session service because we need the admins email to be set 
        SetupFeatureFlag(true);
        const string userEmail = "super@example.com";
        List<string> superAdminEmails = [userEmail];
        
        var mockConfigurationSection = new Mock<IConfigurationSection>();
        var mockEmailSections = superAdminEmails.Select(email =>
        {
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(s => s.Value).Returns(email);
            return mockSection.Object;
        }).ToList();

        var mockSuperAdminSection = new Mock<IConfigurationSection>();
        mockSuperAdminSection.Setup(s => s.GetChildren())
            .Returns(mockEmailSections);

        mockConfigurationSection.Setup(c => c.GetSection("SuperAdminEmails"))
            .Returns(mockSuperAdminSection.Object);

        User user = await CreateAndAddUser(
            "Test User",
            userEmail,
            "test-sram-id",
            PermissionLevel.User
        );
        var userSession = new UserSession
        {
            Email = userEmail,
            Name = "Test User",
            SRAMId = "test-sram-id",
            User = user
        };
        SetupSessionWithUser(userSession);
        
        var sessionService = new UserSessionService(
            _context,
            _mockHttpContextAccessor.Object,
            _mockCollaborationMapper.Object,
            _mockFeatureManager.Object,
            mockConfigurationSection.Object
        );
        
        // Act
        UserSession? result = await sessionService.GetUser();

        // Assert
        Assert.NotNull(result?.User);
        Assert.Equal(PermissionLevel.SuperAdmin, result.User.PermissionLevel);
    
        // Verify the user was updated in the database as well
        User? updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(PermissionLevel.SuperAdmin, updatedUser.PermissionLevel);
    }

    [Fact]
    public async Task SetUser_WhenUserEmailNotInSuperAdminEmails_DoesNotPromote()
    {
        // Arrange
        SetupFeatureFlag(true);

        await CreateAndAddUser(
            "Test User",
            "regular@example.com",
            "test-person-id",
            PermissionLevel.User
        );
        ClaimsPrincipal principal = CreateClaimsPrincipal(
            "test-person-id",
            "Test User",
            "regular@example.com"
        );
        _mockHttpContext.Setup(c => c.User).Returns(principal);

        // Act
        UserSession? result = await _service.SetUser(principal);

        // Assert
        Assert.NotNull(result?.User);
        Assert.Equal(PermissionLevel.User, result.User.PermissionLevel);
    }
}