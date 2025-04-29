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
        // Ensure configuration key matches exactly, including case if necessary
        _allowedRedirects = configuration.GetSection("Authentication:Orcid:AllowedRedirectUris").Get<string[]>() ?? [];
    }
    
    [HttpGet("link")]
    [Authorize] // User must be logged in with primary auth
    public async Task<IActionResult> LinkOrcid([FromQuery] string redirectUri)
    {
        // Validate redirect URL
        string safeRedirectUri = IsValidRedirectUrl(redirectUri) ? redirectUri : "/orcid/redirect"; // Consider a safer default or error

        if (await _featureManager.IsEnabledAsync("OrcidAuthentication"))
        {
            // The RedirectUri for the challenge should be the endpoint that handles the callback from ORCID,
            // which is typically the CallbackPath configured in AddOAuth, not necessarily "/orcid/redirect".
            // Let's assume "/orcid/finalize" is the intended handler based on Program.cs logic.
            // The 'finalRedirect' item will be used *after* successful ORCID authentication and processing in OrcidFinalize.
            return Challenge(new AuthenticationProperties
            {
                // This RedirectUri tells the middleware where to redirect *after* the OrcidCookie is created.
                // It should point to your finalize endpoint.
                RedirectUri = "/orcid/finalize", 
                Items = { { "finalRedirect", safeRedirectUri } } // Pass the *original* desired redirect URI
            }, "orcid");
        }
        
        // This block seems like a fallback/development path and might not be intended for production ORCID linking.
        // It bypasses the actual ORCID flow and hardcodes an ORCID.
        // If SRAMAuthentication is disabled, it just redirects without saving.
        // If SRAMAuthentication is enabled, it tries to save the hardcoded ORCID.
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession == null) 
            return Unauthorized("User not logged in");

        if (userSession.User == null) 
            return BadRequest("User session does not contain a user");

        // Hardcoded ORCID - likely for testing/dev only
        string exampleOrcid = "0000-0002-1825-0097"; 

        // If SRAM is disabled, just redirect to the final destination without linking ORCID via DB.
        if (!await _featureManager.IsEnabledAsync("SRAMAuthentication"))
        {
            // Optionally, you might want to store the ORCID in the session even in this dev path.
            // HttpContext.Session.SetString("orcid", exampleOrcid); 
            return Redirect(safeRedirectUri); // Use the validated safeRedirectUri
        }

        // If SRAM is enabled (but OrcidAuthentication feature flag is off), update DB with hardcoded ORCID.
        // This still needs the fix to avoid the DbUpdateException.
        User? dbUser = await _context.Users.FindAsync(userSession.User.Id);
        if (dbUser == null)
        {
            return NotFound("User not found in database.");
        }

        dbUser.ORCiD = exampleOrcid; 
        // No need for _context.Users.Update(dbUser) when modifying a tracked entity.
        await _context.SaveChangesAsync();

        // Update the session state after saving changes
        await _userSessionService.UpdateUser(); 
        
        // Redirect to the final destination after linking (in this dev path)
        return Redirect(safeRedirectUri); // Use the validated safeRedirectUri
    }

    private bool IsValidRedirectUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        // Allow relative paths starting with '/'
        if (url.StartsWith("/")) return true; 

        // Check if the URL is absolute and in the allowed redirect URLs list
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            // Check against configured allowed origins/redirects
            return _allowedRedirects.Any(allowed => 
                uri.OriginalString.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    [HttpGet("finalize")]
    // No [Authorize] here, as the user is returning from ORCID and authenticated via OrcidCookie
    public async Task<IActionResult> OrcidFinalize([FromQuery] string? redirectUri) // Make redirectUri nullable
    {
        // 1. Get the final redirect URI passed during the challenge
        AuthenticateResult authPropertiesResult = await HttpContext.AuthenticateAsync("OrcidCookie");
        string? finalRedirectUri = authPropertiesResult.Properties?.Items["finalRedirect"];

        // Use the redirectUri from the query as a fallback, but prioritize the one from properties
        string effectiveRedirectUri = finalRedirectUri ?? redirectUri ?? "/"; // Default to root if none provided

        // Validate the effective redirect URL
        string safeRedirectUri = IsValidRedirectUrl(effectiveRedirectUri) ? effectiveRedirectUri : "/"; // Default to root on invalid
        
        // 2. Get the authenticated result from ORCID via the cookie scheme
        AuthenticateResult authenticateResult = await HttpContext.AuthenticateAsync("OrcidCookie"); // Already done above
        if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
        {
             return BadRequest("ORCID authentication failed or principal not found.");
        }

        // 3. Extract the ORCID ID from claims (using the standard NameIdentifier claim type)
        string? orcidId = authenticateResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier); 
        if (string.IsNullOrEmpty(orcidId)) 
        {
             // Fallback to session if claim is missing (though it shouldn't be if configured correctly)
             orcidId = HttpContext.Session.GetString("orcid");
             if (string.IsNullOrEmpty(orcidId))
             {
                return BadRequest("Could not retrieve ORCID ID from claims or session.");
             }
        }

        // 4. Get current primary user session
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession == null || userSession.User == null) 
        {
            // This scenario might happen if the primary session expired while the user was at ORCID.
            // Handle appropriately - maybe redirect to login?
            return Unauthorized("Primary user session not found or invalid. Please log in again.");
        }

        // 5. Fetch the corresponding User entity from the database
        User? dbUser = await _context.Users.FindAsync(userSession.User.Id);
        if (dbUser == null)
        {
            // User exists in session but not DB? This indicates an inconsistency.
            return NotFound($"User with ID {userSession.User.Id} not found in database.");
        }

        // 6. Update *only* the ORCiD property on the tracked entity
        dbUser.ORCiD = orcidId;

        // 7. Save changes for the single property update
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Log the detailed exception
            Console.WriteLine($"Error saving ORCID link: {ex.ToString()}"); 
            // Provide a generic error to the client
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while linking the ORCID.");
        }

        // 8. Update the user session state to reflect the change
        await _userSessionService.UpdateUser();

        // 9. Redirect to the final, validated destination
        return Redirect(safeRedirectUri);
    }
    
    [HttpGet("unlink")]
    [Authorize] // User must be logged in with primary auth
    public async Task<IActionResult> OrcidUnlink()
    {
        // Get current user session
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession == null || userSession.User == null) 
            return Unauthorized("User not logged in or session invalid.");

        // Fetch the corresponding User entity from the database
        User? dbUser = await _context.Users.FindAsync(userSession.User.Id);
        if (dbUser == null)
        {
            return NotFound($"User with ID {userSession.User.Id} not found in database.");
        }

        // Update *only* the ORCiD property
        dbUser.ORCiD = null;

        // Save changes
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
             Console.WriteLine($"Error saving ORCID unlink: {ex.ToString()}");
             return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while unlinking the ORCID.");
        }

        // Update the user session state
        await _userSessionService.UpdateUser();

        return Ok("ORCID successfully unlinked.");
    }
    
    // This endpoint might not be strictly necessary if RedirectUri in Challenge points directly to finalize.
    // If kept, ensure it doesn't interfere with the flow.
    [HttpGet("redirect")]
    public IActionResult OrcidRedirect() =>
        // This just has to be a valid endpoint. It might be where ORCID initially redirects *before*
        // the cookie middleware processes the request and redirects again to `finalize`.
        Ok("ORCID redirect endpoint reached. Processing..."); 
}