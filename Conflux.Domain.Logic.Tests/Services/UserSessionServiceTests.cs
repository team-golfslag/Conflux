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
        UserSession? result = await _service.GetSession();

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
        UserSession? result = await _service.GetSession();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.Name);
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
        
        // Create a user in the database
        var userId = Guid.CreateVersion7();
        var user = new User
        {
            Id = userId,
            SRAMId = "test-person-id",
            PersonId = Guid.CreateVersion7(),
            SCIMId = "test-scim-id",
            PermissionLevel = PermissionLevel.User
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        // Create a user session and set it up
        var userSession = new UserSession
        {
            SRAMId = "test-person-id",
            Name = "Test User",
            GivenName = "Test",
            FamilyName = "User",
            Email = "test@example.com",
            UserId = userId
        };
        
        // Mock GetUserSession to return our userSession
        ClaimsPrincipal principal = CreateClaimsPrincipal(
            "test-person-id",
            "Test User",
            "test@example.com"
        );
        _mockHttpContext.Setup(c => c.User).Returns(principal);
        _mockCollaborationMapper
            .Setup(m => m.Map(It.IsAny<List<CollaborationDTO>>()))
            .ReturnsAsync(new List<Collaboration>());
            
        // Mock the session methods
        SetupSessionWithUser(userSession);

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
    
    // Updated test with a working approach
    [Fact]
    public async Task GetUser_WhenUserEmailInSuperAdminEmails_PromotesToSuperAdmin()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options =
            new DbContextOptionsBuilder<ConfluxContext>()
                .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
                .Options;
        var context = new ConfluxContext(options);
        
        SetupFeatureFlag(true);
        const string userEmail = "super@example.com";
        
        var mockEmailSection = new Mock<IConfigurationSection>();
        mockEmailSection.Setup(s => s.Value).Returns(userEmail);
        
        var sections = new List<IConfigurationSection> { mockEmailSection.Object };
        
        var mockSuperAdminSection = new Mock<IConfigurationSection>();
        mockSuperAdminSection.Setup(s => s.GetChildren()).Returns(sections);
        
        var mockConfigurationSection = new Mock<IConfiguration>();
        mockConfigurationSection.Setup(c => c.GetSection("SuperAdminEmails"))
            .Returns(mockSuperAdminSection.Object);
        
        // Create and add user
        var personId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var person = new Person 
        { 
            Id = personId, 
            Name = "Test User", 
            Email = userEmail
        };
        
        var user = new User
        {
            Id = userId,
            SRAMId = "test-sram-id",
            PersonId = personId,
            Person = person,
            SCIMId = "test-scim-id",
            PermissionLevel = PermissionLevel.User
        };
        
        context.People.Add(person);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        // Create user session
        var userSession = new UserSession
        {
            Email = userEmail,
            Name = "Test User",
            SRAMId = "test-sram-id",
            UserId = userId
        };
        
        // Mock session methods
        var mockSession = new Mock<ISession>();
        byte[]? serializedSession = JsonSerializer.SerializeToUtf8Bytes(userSession);
        mockSession.Setup(s => s.TryGetValue("UserProfile", out serializedSession))
            .Returns(true);
        mockSession.Setup(s => s.IsAvailable).Returns(true);
        
        // Mock HTTP context
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);
        
        // Create service with new dependencies
        var sessionService = new UserSessionService(
            context,
            mockHttpContextAccessor.Object,
            _mockCollaborationMapper.Object,
            _mockFeatureManager.Object,
            mockConfigurationSection.Object
        );
        
        // Act
        UserSession? result = await sessionService.GetSession();
        User updatedUser = await context.Users.FindAsync(userId);
        
        // Assert
        Assert.NotNull(updatedUser);
        Assert.Equal(PermissionLevel.SuperAdmin, updatedUser.PermissionLevel);
        
        // Cleanup
        context.Dispose();
    }

    [Fact]
    public async Task SetUser_WhenUserEmailNotInSuperAdminEmails_DoesNotPromote()
    {
        // Arrange
        SetupFeatureFlag(true);

        // Create and add user
        User user = await CreateAndAddUser(
            "Test User",
            "regular@example.com",
            "test-person-id",
            PermissionLevel.User
        );
        
        // Create user session
        var userSession = new UserSession
        {
            SRAMId = "test-person-id",
            Name = "Test User",
            GivenName = "Test",
            FamilyName = "User",
            Email = "regular@example.com",
            UserId = user.Id
        };
        
        // Set up the session
        SetupSessionWithUser(userSession);
        
        ClaimsPrincipal principal = CreateClaimsPrincipal(
            "test-person-id",
            "Test User",
            "regular@example.com"
        );
        _mockHttpContext.Setup(c => c.User).Returns(principal);
        _mockCollaborationMapper
            .Setup(m => m.Map(It.IsAny<List<CollaborationDTO>>()))
            .ReturnsAsync(new List<Collaboration>());

        // Act
        UserSession? result = await _service.SetUser(principal);
        User updatedUser = await _context.Users.FindAsync(user.Id);

        // Assert
        Assert.NotNull(updatedUser);
        Assert.Equal(PermissionLevel.User, updatedUser.PermissionLevel);
    }
}