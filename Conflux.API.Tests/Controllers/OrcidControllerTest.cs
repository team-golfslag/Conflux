// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using System.Text;
using Conflux.API.Controllers;
using Conflux.Data;
using Conflux.Domain;
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
using Xunit;

namespace Conflux.API.Tests.Controllers;

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
    private ConfluxContext _context = null!;     // Initialize in each test
    private OrcidController _controller = null!; // Initialize in each test

    public OrcidControllerTests()
    {
        _mockFeatureManager = new();
        _mockUserSessionService = new();
        _mockConfiguration = new();
        _mockConfigSection = new();

        _dbOptions = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique DB for each test run
            .Options;

        // Setup Configuration mock
        _mockConfigSection.Setup(s => s.Value)
            .Returns(string.Join(",", _allowedRedirects));
        _mockConfiguration.Setup(c => c.GetSection("Authentication:Orcid:AllowedRedirectUris"))
            .Returns(_mockConfigSection.Object);
    }

    private void InitializeController(User? user = null, UserSession? session = null)
    {
        _context = new(_dbOptions);
        if (user != null)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        _controller = new(_context, _mockFeatureManager.Object, _mockUserSessionService.Object,
            _mockConfiguration.Object);

        DefaultHttpContext httpContext = new();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationService))).Returns(mockAuthService.Object);
        httpContext.RequestServices = mockServiceProvider.Object;
        httpContext.Session = new Mock<ISession>().Object; // Add mock session

        _controller.ControllerContext = new()
        {
            HttpContext = httpContext,
        };

        if (session != null)
            _mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(session);
        else
            _mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync((UserSession?)null);
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

        var mockSession = Mock.Get(_controller.HttpContext.Session);
        if (orcidSession != null)
        {
            byte[]? orcidBytes = Encoding.UTF8.GetBytes(orcidSession);
            mockSession.Setup(s => s.TryGetValue("orcid", out orcidBytes)).Returns(true);
        }
        else
        {
            byte[]? nullBytes = null;
            mockSession.Setup(s => s.TryGetValue("orcid", out nullBytes)).Returns(false);
        }
    }

    // --- LinkOrcid Tests ---

    [Fact]
    public async Task LinkOrcid_WhenOrcidAuthEnabledAndUserLoggedInAndRedirectValid_ReturnsChallengeResult()
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            SCIMId = "scimid",
            Name = "testuser",
        };
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
    public async Task LinkOrcid_WhenOrcidAuthDisabledSramAuthEnabledAndUserLoggedIn_UpdatesOrcidAndRedirects()
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            ORCiD = null,
            SCIMId = "scimid",
            Name = "testuser",
        };
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(false);
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        IActionResult result = await _controller.LinkOrcid(ValidRelativeRedirect);

        RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(ValidRelativeRedirect, redirectResult.Url);

        User? dbUser = await _context.Users.FindAsync(user.Id);
        Assert.Equal(ExampleOrcid, dbUser?.ORCiD);                       // Check if hardcoded ORCID was saved
        _mockUserSessionService.Verify(s => s.UpdateUser(), Times.Once); // Verify session update
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
    public async Task LinkOrcid_WhenOrcidAuthDisabledSramAuthEnabledAndUserNotInDb_ReturnsNotFound()
    {
        // User exists in session but not in DB (inconsistent state)
        User sessionUser = new()
        {
            Id = Guid.NewGuid(),
            ORCiD = null,
            SCIMId = null,
            Name = "ghost",
        };
        UserSession session = new()
        {
            User = sessionUser,
        };
        InitializeController(session: session); // Don't add user to DB
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(false);
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        IActionResult result = await _controller.LinkOrcid(ValidAbsoluteRedirect);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task LinkOrcid_WhenRedirectUrlInvalid_UsesDefaultRedirectInChallenge()
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            Name = "testuser",
            SCIMId = "scimid",
        };
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        IActionResult result = await _controller.LinkOrcid(InvalidRedirect);

        ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal(DefaultRedirect, challengeResult.Properties?.Items["finalRedirect"]);
    }

    [Fact]
    public async Task LinkOrcid_WhenRedirectUrlRelative_UsesRelativeUrlInChallenge()
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            Name = "testuser",
            SCIMId = "scimid",
        };
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        _mockFeatureManager.Setup(fm => fm.IsEnabledAsync("OrcidAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        IActionResult result = await _controller.LinkOrcid(ValidRelativeRedirect);

        ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal(ValidRelativeRedirect, challengeResult.Properties?.Items["finalRedirect"]);
    }

    // --- OrcidFinalize Tests ---

    [Fact]
    public async Task OrcidFinalize_WhenAuthenticationFails_ReturnsUnauthorized()
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            Name = "testuser",
            SCIMId = "scimid",
        };
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
    public async Task OrcidFinalize_WhenUserSessionHasNoUser_ReturnsBadRequest()
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
        User sessionUser = new()
        {
            Id = Guid.Empty,
            SCIMId = null!,
            Name = "ghost",
        };
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
    public async Task OrcidFinalize_WhenFinalRedirectMissing_UsesDefaultRedirect()
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            Name = "testuser",
            SCIMId = "scimid",
        };
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);
        SetupAuthentication(ExampleOrcid); // Simulate successful ORCID auth

        // Simulate properties *without* finalRedirect
        AuthenticationProperties authProps = new(); // Empty properties
        var mockAuthService = Mock.Get(_controller.HttpContext.RequestServices.GetService<IAuthenticationService>()!);
        ClaimsPrincipal claimsPrincipal =
            new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, ExampleOrcid) },
                "OrcidCookie"));
        AuthenticationTicket authTicket = new(claimsPrincipal, authProps, "OrcidCookie");
        AuthenticateResult authResult = AuthenticateResult.Success(authTicket);

        mockAuthService.Setup(x => x.AuthenticateAsync(_controller.HttpContext, "OrcidCookie"))
            .ReturnsAsync(authResult);

        IActionResult result = await _controller.OrcidFinalize(ValidAbsoluteRedirect);

        RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(DefaultRedirect, redirectResult.Url); // Check redirect uses default

        User? dbUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(dbUser);
        Assert.Equal(ExampleOrcid, dbUser.ORCiD);                        // Check ORCID update
        _mockUserSessionService.Verify(s => s.UpdateUser(), Times.Once); // Verify session update
    }

    [Fact]
    public async Task OrcidUnlink_WhenUserLoggedInAndHasOrcid_RemovesOrcidAndReturnsOk()
    {
        // Create user with ORCID set
        User user = new()
        {
            Id = Guid.NewGuid(),
            Name = "testuser",
            SCIMId = "scimid",
            ORCiD = ExampleOrcid,
        };
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);

        IActionResult result = await _controller.OrcidUnlink();

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("ORCID successfully unlinked.", okResult.Value);

        User? dbUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(dbUser);
        Assert.Null(dbUser.ORCiD);                                              // ORCID should be null after unlinking
        _mockUserSessionService.Verify(s => s.CommitUser(session), Times.Once); // Verify session update
    }

    [Fact]
    public async Task OrcidUnlink_WhenUserLoggedInAndHasNoOrcid_ReturnsOkButMakesNoChanges()
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            Name = "testuser",
            SCIMId = "scimid",
            ORCiD = null, // No ORCID
        };
        UserSession session = new()
        {
            User = user,
        };
        InitializeController(user, session);

        IActionResult result = await _controller.OrcidUnlink();

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("ORCID successfully unlinked.", okResult.Value);

        User? dbUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(dbUser);
        Assert.Null(dbUser.ORCiD);                                              // ORCID should remain null
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
        User sessionUser = new()
        {
            Id = Guid.NewGuid(),
            ORCiD = ExampleOrcid,
            SCIMId = "scimid",
            Name = "ghost",
        };
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
