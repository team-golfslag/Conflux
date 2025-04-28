// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace Conflux.API.Controllers;

[ApiController]
[Route("orcid")]
public class OrcidController : ControllerBase
{
    private readonly string[] _allowedRedirects;
    private readonly IVariantFeatureManager _featureManager;
    private readonly ConfluxContext _context;
    private readonly IUserSessionService _userSessionService;
    
    public OrcidController(ConfluxContext context, IVariantFeatureManager featureManager, IUserSessionService userSessionService, IConfiguration configuration)
    {
        _context = context;
        _userSessionService = userSessionService;
        _featureManager = featureManager;
        _allowedRedirects = configuration.GetSection("Authentication:ORCID:AllowedRedirectUris").Get<string[]>() ?? [];
    }
    
    [HttpGet("link")]
    [Authorize] // User must be logged in with primary auth
    public IActionResult LinkOrcid([FromQuery] string redirectUri)
    {
        // Validate redirect URL
        string safeRedirectUri = IsValidRedirectUrl(redirectUri) ? redirectUri : "/orcid/redirect";
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = "/orcid/redirect",
            // Store additional info if needed
            Items = { { "finalRedirect", safeRedirectUri } }
        }, "orcid"); // Use the ORCID scheme name
    }

    private bool IsValidRedirectUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        // Check if the URL is in the allowed redirect URLs list
        return _allowedRedirects.Any(allowed =>
            url.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));
    }

    [HttpGet("finalize")]
    public async Task<IActionResult> OrcidFinalize([FromQuery] string redirectUri)
    {
        // Validate redirect URL
        string safeRedirectUri = IsValidRedirectUrl(redirectUri) ? redirectUri : "";
        if (string.IsNullOrEmpty(safeRedirectUri))
            return BadRequest("Invalid redirect URL");
        
        // Get the authenticated result from ORCID
        AuthenticateResult authenticateResult = await HttpContext.AuthenticateAsync("OrcidCookie");
        if (!authenticateResult.Succeeded)
            return BadRequest("ORCID authentication failed: " + 
                (authenticateResult.Failure?.Message ?? "Unknown reason"));

        // Extract the ORCID ID from claims
        string? orcidId = authenticateResult.Principal.FindFirstValue("sub");
        if (string.IsNullOrEmpty(orcidId)) 
            return BadRequest("Could not retrieve ORCID ID from claims");

        // Get current user
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession == null) 
            return Unauthorized("User not logged in");

        if (userSession.User == null) 
            return BadRequest("User session does not contain a user");

        userSession.User.ORCiD = orcidId;

        if (!await _featureManager.IsEnabledAsync("SRAMAuthentication"))
        {
            userSession.User.ORCiD = orcidId;
            Console.WriteLine($"User {userSession.User.Name} linked ORCID ID: {orcidId}");
            return Redirect(redirectUri);
        }

        _context.Users.Update(userSession.User);
        await _context.SaveChangesAsync();

        return Redirect(redirectUri);
    }
    
    [HttpGet("redirect")]
    public IActionResult OrcidRedirect() =>
        // This just has to be a valid endpoint to redirect to after ORCID authentication
        Ok();
}