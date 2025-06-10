// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using System.Text;
using Conflux.API.Controllers;
using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moq;
using ORCID.Net.Models;
using ORCID.Net.Services;
using Xunit;

namespace Conflux.API.Tests.Controllers;

// Define a delegate type for mocking out parameters
public delegate bool TryGetValueDelegate(string key, out byte[] value);

public class OrcidControllerTests
{
    private const string DefaultRedirect = "/orcid/redirect";
    private const string ValidAbsoluteRedirect = "https://allowed.example.com/callback";
    private const string ValidRelativeRedirect = "/relative/path?state=456";
    private const string InvalidRedirect = "https://disallowed.example.com";
    private const string ExampleOrcid = "0000-0002-1825-0097"; // Hardcoded in controller fallback
    private readonly string[] _allowedRedirects = ["https://allowed.example.com/callback", "/relative/path"];
    private readonly DbContextOptions<ConfluxContext> _dbOptions;
    private readonly Mock<IConfigurationSection> _mockConfigSection;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IVariantFeatureManager> _mockFeatureManager;
    private readonly Mock<IUserSessionService> _mockUserSessionService;
    private readonly Mock<IPersonRetrievalService> _mockPersonRetrievalService;
    private readonly Mock<IPeopleService> _mockPeopleService;
    private ConfluxContext _context = null!;     // Initialize in each test
    private OrcidController _controller = null!; // Initialize in each test

    public OrcidControllerTests()
    {
        _mockFeatureManager = new();
        _mockUserSessionService = new();
        _mockPersonRetrievalService = new();
        _mockPeopleService = new();
        _mockConfiguration = new();
        _mockConfigSection = new();

        _dbOptions = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique DB for each test run
            .Options;

        // Setup Configuration mock
        // We can't use extension method .Get<string[]>() in Moq
        // Instead, create a new section mock specifically for the allowed redirects
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.Value).Returns(string.Join(",", _allowedRedirects));
        _mockConfiguration.Setup(c => c.GetSection("Authentication:Orcid:AllowedRedirectUris"))
            .Returns(mockConfigSection.Object);
    }

    private void InitializeController(User? user = null, UserSession? session = null)
    {
        _context = new(_dbOptions);
        if (user != null)
        {
            // Add both User and Person to context
            _context.People.Add(user.Person);
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        _controller = new(_context, _mockFeatureManager.Object, _mockUserSessionService.Object,
            _mockConfiguration.Object, _mockPeopleService.Object, _mockPersonRetrievalService.Object);

        DefaultHttpContext httpContext = new();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationService))).Returns(mockAuthService.Object);
        httpContext.RequestServices = mockServiceProvider.Object;

        // Create a mock session
        var mockSession = new Mock<ISession>();
        httpContext.Session = mockSession.Object;

        _controller.ControllerContext = new()
        {
            HttpContext = httpContext,
        };

        if (session != null)
            _mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(session);
        else
            _mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync((UserSession?)null);
    }

    private static User CreateUserWithPerson(string name, string scimId, string? orcid = null, Guid? userId = null,
        Guid? personId = null)
    {
        var actualUserId = userId ?? Guid.NewGuid();
        var actualPersonId = personId ?? Guid.NewGuid();

        // First create the person without the User reference
        var person = new Person
        {
            Id = actualPersonId,
            Name = name,
            ORCiD = orcid,
            User = null
        };

        // Now create the user with the person reference
        var user = new User
        {
            Id = actualUserId,
            SCIMId = scimId,
            PersonId = actualPersonId,
            Person = person
        };

        // Set the bidirectional reference
        person.User = user;

        return user;
    }

    private void SetupAuthentication(string? orcidClaim = null, string? orcidSession = null)
    {
        var claims = new List<Claim>();
        if (orcidClaim != null) claims.Add(new(ClaimTypes.NameIdentifier, orcidClaim));

        ClaimsIdentity claimsIdentity = new(claims, "OrcidCookie");
        ClaimsPrincipal claimsPrincipal = new(claimsIdentity);

        AuthenticateResult authResult = orcidClaim != null
            ? AuthenticateResult.Success(new(claimsPrincipal, "OrcidCookie"))
            : AuthenticateResult.Fail("No ORCID claim");

        var mockAuthService = Mock.Get(_controller.HttpContext.RequestServices.GetService<IAuthenticationService>()!);
        mockAuthService.Setup(x => x.AuthenticateAsync(_controller.HttpContext, "OrcidCookie"))
            .ReturnsAsync(authResult);

        // We need to mock the Session differently since GetString is an extension method
        var mockSession = new Mock<ISession>();

        if (orcidSession != null)
        {
            byte[] orcidBytes = Encoding.UTF8.GetBytes(orcidSession);

            // Use our custom delegate for TryGetValue
            mockSession.Setup(s => s.TryGetValue("orcid", out It.Ref<byte[]>.IsAny))
                .Returns(new TryGetValueDelegate((string key, out byte[] value) =>
                {
                    value = orcidBytes;
                    return true;
                }));
        }
        else
        {
            // Use our custom delegate for TryGetValue with no value
            mockSession.Setup(s => s.TryGetValue("orcid", out It.Ref<byte[]>.IsAny))
                .Returns(new TryGetValueDelegate((string key, out byte[] value) =>
                {
                    value = Array.Empty<byte>();
                    return false;
                }));
        }

        _controller.ControllerContext.HttpContext.Session = mockSession.Object;
    }

    // --- LinkOrcid Tests ---

    [Fact]
    public async Task LinkOrcid_WhenOrcidAuthEnabledAndUserLoggedInAndRedirectValid_ReturnsChallengeResult()
    {
        User user = CreateUserWithPerson("testuser", "scimid");
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        IActionResult result = await _controller.LinkOrcid(ValidAbsoluteRedirect);

        ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal("orcid", challengeResult.AuthenticationSchemes.Single());
        Assert.Equal("/orcid/finalize", challengeResult.Properties?.RedirectUri);
    }

    [Fact]
    public async Task LinkOrcid_WhenOrcidAuthDisabledAndUserLoggedIn_UpdatesOrcidAndRedirects()
    {
        User user = CreateUserWithPerson("testuser", "scimid");
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(false);

        IActionResult result = await _controller.LinkOrcid(ValidRelativeRedirect);

        RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(ValidRelativeRedirect, redirectResult.Url);

        User? dbUser = await _context.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.Equal("https://orcid.org/" + ExampleOrcid, dbUser?.Person?.ORCiD); // Check if hardcoded ORCID was saved
        _mockUserSessionService.Verify(s => s.UpdateUser(), Times.Once);          // Verify session update
    }

    [Fact]
    public async Task LinkOrcid_WhenUserNotLoggedIn_ReturnsUnauthorized()
    {
        InitializeController(); // No user/session
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(false); // Fallback path checks session first

        IActionResult result = await _controller.LinkOrcid(ValidAbsoluteRedirect);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task LinkOrcid_WhenUserSessionHasNoUser_ReturnsBadRequest()
    {
        UserSession session = new()
        {
            User = null,
        }; // Session exists, but User is null
        InitializeController(session: session);
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(false); // Fallback path

        IActionResult result = await _controller.LinkOrcid(ValidAbsoluteRedirect);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task LinkOrcid_WhenUserNotInDb_ReturnsNotFound()
    {
        // User exists in session but not in DB (inconsistent state)
        User sessionUser = CreateUserWithPerson("ghost", "scimid");
        UserSession session = new()
        {
            User = sessionUser,
        };
        InitializeController(session: session); // Don't add user to DB
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(false);

        IActionResult result = await _controller.LinkOrcid(ValidAbsoluteRedirect);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task LinkOrcid_WhenRedirectUrlInvalid_UsesDefaultRedirectInChallenge()
    {
        User user = CreateUserWithPerson("testuser", "scimid");
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        IActionResult result = await _controller.LinkOrcid(InvalidRedirect);

        ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal("/orcid/finalize", challengeResult.Properties?.RedirectUri);
        Assert.Equal(DefaultRedirect, challengeResult.Properties?.Items["finalRedirect"]);
    }

    [Fact]
    public async Task LinkOrcid_WhenRedirectUrlRelative_UsesRelativeUrlInChallenge()
    {
        User user = CreateUserWithPerson("testuser", "scimid");
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        IActionResult result = await _controller.LinkOrcid(ValidRelativeRedirect);

        ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal("/orcid/finalize", challengeResult.Properties?.RedirectUri);
        Assert.Equal(ValidRelativeRedirect, challengeResult.Properties?.Items["finalRedirect"]);
    }

    // --- OrcidFinalize Tests ---

    [Fact]
    public async Task OrcidFinalize_WhenAuthenticationFails_ReturnsBadRequest()
    {
        User user = CreateUserWithPerson("testuser", "scimid");
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        SetupAuthentication(); // Simulate failed ORCID auth (no claim)

        IActionResult result = await _controller.OrcidFinalize(ValidAbsoluteRedirect);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockUserSessionService.Verify(s => s.UpdateUser(), Times.Never); // No update should occur
    }

    [Fact]
    public async Task OrcidFinalize_WhenUserNotLoggedIn_ReturnsUnauthorized()
    {
        InitializeController(); // No user/session
        // Need to setup auth context even if session is null, as controller checks auth first
        SetupAuthentication(ExampleOrcid);

        IActionResult result = await _controller.OrcidFinalize(ValidRelativeRedirect);

        // Controller checks session *after* authentication result
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task OrcidFinalize_WhenUserSessionHasNoUser_ReturnsUnauthorized()
    {
        UserSession session = new()
        {
            User = null,
        }; // Session exists, but User is null
        InitializeController(session: session);
        SetupAuthentication(ExampleOrcid); // Simulate successful ORCID auth

        IActionResult result = await _controller.OrcidFinalize(ValidAbsoluteRedirect);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task OrcidFinalize_WhenUserNotInDb_ReturnsNotFound()
    {
        // User exists in session but not in DB (inconsistent state)
        User sessionUser = CreateUserWithPerson("ghost", "scimid");
        UserSession session = new()
        {
            User = sessionUser,
        };
        InitializeController(session: session); // Don't add user to DB
        SetupAuthentication(ExampleOrcid);      // Simulate successful ORCID auth

        IActionResult result = await _controller.OrcidFinalize(ValidAbsoluteRedirect);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task OrcidFinalize_WhenSuccessful_UpdatesOrcidAndRedirects()
    {
        User user = CreateUserWithPerson("testuser", "scimid");
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        SetupAuthentication(ExampleOrcid); // Simulate successful ORCID auth

        IActionResult result = await _controller.OrcidFinalize(ValidAbsoluteRedirect);

        RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(ValidAbsoluteRedirect, redirectResult.Url);

        User? dbUser = await _context.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(dbUser);
        Assert.Equal("https://orcid.org/" + ExampleOrcid, dbUser!.Person?.ORCiD); // Check ORCID update
        _mockUserSessionService.Verify(s => s.UpdateUser(), Times.Once);          // Verify session update
    }

    [Fact]
    public async Task OrcidFinalize_WithSessionOrcid_UpdatesOrcidAndRedirects()
    {
        User user = CreateUserWithPerson("testuser", "scimid");
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        // Claim missing but session has ORCID value
        SetupAuthentication(null, ExampleOrcid);

        IActionResult result = await _controller.OrcidFinalize(ValidRelativeRedirect);

        RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(ValidRelativeRedirect, redirectResult.Url);

        User? dbUser = await _context.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(dbUser);
        Assert.Equal("https://orcid.org/" + ExampleOrcid, dbUser!.Person?.ORCiD); // Check ORCID update
        _mockUserSessionService.Verify(s => s.UpdateUser(), Times.Once);          // Verify session update
    }

    // --- GetPersonByQuery Tests ---

    [Fact]
    public async Task GetPersonByQuery_WhenNoPersonsFound_ReturnsEmptyList()
    {
        // Arrange
        InitializeController();
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidIntegration", CancellationToken.None))
            .ReturnsAsync(true);
        _mockPersonRetrievalService.Setup(s => s.FindPeopleByNameFast(It.IsAny<string>()))
            .ReturnsAsync(new List<OrcidPerson>());

        // Act
        var result = await _controller.GetPersonByQuery("test query");

        // Assert
        var returnValue = Assert.IsType<ActionResult<List<Person>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(returnValue.Result);
        var persons = Assert.IsType<List<Person>>(okResult.Value);
        Assert.Empty(persons);
    }

    [Fact]
    public async Task GetPersonByQuery_WhenPersonsFound_CreatesPeopleAndReturns()
    {
        // Arrange
        InitializeController();
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidIntegration", CancellationToken.None))
            .ReturnsAsync(true);
        var orcidPerson = new OrcidPerson("John", "Doe", "John Doe", "", ExampleOrcid);

        _mockPersonRetrievalService.Setup(s => s.FindPeopleByNameFast(It.IsAny<string>()))
            .ReturnsAsync(new List<OrcidPerson>
            {
                orcidPerson
            });

        // Setup that person doesn't already exist
        _mockPeopleService.Setup(s => s.GetPersonByOrcidIdAsync(ExampleOrcid))
            .ReturnsAsync((Person?)null);

        // Setup person creation
        var newPerson = new Person
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            GivenName = "John",
            FamilyName = "Doe",
            ORCiD = ExampleOrcid
        };

        _mockPeopleService.Setup(s => s.CreatePersonAsync(It.IsAny<PersonRequestDTO>()))
            .ReturnsAsync(newPerson);

        // Act
        var result = await _controller.GetPersonByQuery("John");

        // Assert
        var returnValue = Assert.IsType<ActionResult<List<Person>>>(result);
        Assert.Equal(newPerson, returnValue.Value![0]); // The list is returned directly 
        Assert.Single(returnValue.Value);
        Assert.Equal("John Doe", returnValue.Value[0].Name);
        Assert.Equal(ExampleOrcid, returnValue.Value[0].ORCiD);
    }

    [Fact]
    public async Task GetPersonByQuery_WhenPersonAlreadyExists_SkipsCreation()
    {
        // Arrange
        InitializeController();
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidIntegration", CancellationToken.None))
            .ReturnsAsync(true);
        var orcidPerson = new OrcidPerson("John", "Doe", "John Doe", "", ExampleOrcid);

        _mockPersonRetrievalService.Setup(s => s.FindPeopleByNameFast(It.IsAny<string>()))
            .ReturnsAsync(new List<OrcidPerson>
            {
                orcidPerson
            });

        // Setup that person already exists
        var existingPerson = new Person
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            ORCiD = ExampleOrcid
        };

        _mockPeopleService.Setup(s => s.GetPersonByOrcidIdAsync(ExampleOrcid))
            .ReturnsAsync(existingPerson);

        // Act
        var result = await _controller.GetPersonByQuery("John");

        // Assert
        var returnValue = Assert.IsType<ActionResult<List<Person>>>(result);
        var people = returnValue.Value;
        Assert.NotNull(people);
        Assert.Single(people);                   // Should contain the existing person
        Assert.Equal(existingPerson, people[0]); // The existing person should be in the result
        _mockPeopleService.Verify(s => s.CreatePersonAsync(It.IsAny<PersonRequestDTO>()), Times.Never);
    }

    // --- GetPersonFromOrcid Tests ---

    [Fact]
    public async Task GetPersonFromOrcid_WhenPersonExists_ReturnsPerson()
    {
        // Arrange
        InitializeController();
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidIntegration", CancellationToken.None))
            .ReturnsAsync(true);
        var existingPerson = new Person
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            ORCiD = ExampleOrcid
        };

        _mockPeopleService.Setup(s => s.GetPersonByOrcidIdAsync(ExampleOrcid))
            .ReturnsAsync(existingPerson);

        // Act
        var result = await _controller.GetPersonFromOrcid(ExampleOrcid);

        // Assert
        var actionResult = Assert.IsType<ActionResult<Person>>(result);
        Assert.Equal(existingPerson, actionResult.Value);
        Assert.Equal(ExampleOrcid, actionResult.Value!.ORCiD);
        Assert.Equal("John Doe", actionResult.Value.Name);
    }

    [Fact]
    public async Task GetPersonFromOrcid_WhenPersonDoesNotExist_CreatesAndReturnsPerson()
    {
        // Arrange
        InitializeController();
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidIntegration", CancellationToken.None))
            .ReturnsAsync(true);

        // Person doesn't exist yet
        _mockPeopleService.Setup(s => s.GetPersonByOrcidIdAsync(ExampleOrcid))
            .ReturnsAsync((Person?)null);

        // Setup ORCID API response
        var orcidPerson = new OrcidPerson("John", "Doe", "John Doe", "", ExampleOrcid);

        _mockPersonRetrievalService.Setup(s => s.FindPersonByOrcid(ExampleOrcid))
            .ReturnsAsync(orcidPerson);

        // Setup person creation
        var newPerson = new Person
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            GivenName = "John",
            FamilyName = "Doe",
            ORCiD = ExampleOrcid
        };
        _mockPeopleService.Setup(s => s.CreatePersonAsync(It.IsAny<PersonRequestDTO>()))
            .ReturnsAsync(newPerson);

        // Act
        var result = await _controller.GetPersonFromOrcid(ExampleOrcid);

        // Assert
        var actionResult = Assert.IsType<ActionResult<Person>>(result);
        Assert.Equal(newPerson, actionResult.Value);
        Assert.Equal(ExampleOrcid, actionResult.Value!.ORCiD);
        Assert.Equal("John Doe", actionResult.Value.Name);
        _mockPeopleService.Verify(s => s.CreatePersonAsync(It.IsAny<PersonRequestDTO>()), Times.Once);
    }

    // --- OrcidUnlink Tests ---

    [Fact]
    public async Task OrcidUnlink_WhenUserLoggedInAndHasOrcid_RemovesOrcidAndReturnsOk()
    {
        // Create user with ORCID set
        User user = CreateUserWithPerson("testuser", "scimid");
        user.Person!.ORCiD = ExampleOrcid; // Set ORCID on the Person
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);

        IActionResult result = await _controller.OrcidUnlink();

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("ORCID successfully unlinked.", okResult.Value);

        User? dbUser = await _context.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(dbUser);
        Assert.Null(dbUser!.Person?.ORCiD);                                     // ORCID should be null after unlinking
        _mockUserSessionService.Verify(s => s.CommitUser(session), Times.Once); // Verify session update
    }

    [Fact]
    public async Task OrcidUnlink_WhenUserLoggedInAndHasNoOrcid_ReturnsOkButMakesNoChanges()
    {
        User user = CreateUserWithPerson("testuser", "scimid");
        // user.Person.ORCiD is already null by default
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);

        IActionResult result = await _controller.OrcidUnlink();

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("ORCID successfully unlinked.", okResult.Value);

        User? dbUser = await _context.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(dbUser);
        Assert.Null(dbUser!.Person?.ORCiD);                                     // ORCID should remain null
        _mockUserSessionService.Verify(s => s.CommitUser(session), Times.Once); // Session update still occurs
    }

    [Fact]
    public async Task OrcidUnlink_WhenUserNotLoggedIn_ReturnsUnauthorized()
    {
        InitializeController(); // No user/session

        IActionResult result = await _controller.OrcidUnlink();

        Assert.IsType<UnauthorizedObjectResult>(result);
        _mockUserSessionService.Verify(s => s.UpdateUser(), Times.Never); // No update should occur
    }

    [Fact]
    public async Task OrcidUnlink_WhenUserSessionHasNoUser_ReturnsUnauthorized()
    {
        UserSession session = new()
        {
            User = null,
        };
        InitializeController(session: session);

        IActionResult result = await _controller.OrcidUnlink();

        Assert.IsType<UnauthorizedObjectResult>(result);
        _mockUserSessionService.Verify(s => s.UpdateUser(), Times.Never); // No update should occur
    }

    [Fact]
    public async Task OrcidUnlink_WhenUserNotInDb_ReturnsNotFound()
    {
        // User exists in session but not in DB
        User sessionUser = CreateUserWithPerson("ghost", "scimid");
        sessionUser.Person!.ORCiD = ExampleOrcid;
        UserSession session = new()
        {
            User = sessionUser,
        };
        InitializeController(session: session); // Don't add user to DB

        IActionResult result = await _controller.OrcidUnlink();

        Assert.IsType<NotFoundObjectResult>(result);
        _mockUserSessionService.Verify(s => s.UpdateUser(), Times.Never); // No update should occur
    }
}
