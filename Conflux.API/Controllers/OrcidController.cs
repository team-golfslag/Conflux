// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using ORCID.Net.Models;
using ORCID.Net.ORCIDServiceExceptions;
using ORCID.Net.Services;

namespace Conflux.API.Controllers;

[ApiController]
[Authorize]
[Route("orcid")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public class OrcidController : ControllerBase
{
    private readonly string[] _allowedRedirects;
    private readonly ConfluxContext _context;
    private readonly IVariantFeatureManager _featureManager;
    private readonly IPeopleService _peopleService;
    private readonly IPersonRetrievalService _personRetrievalService;
    private readonly IUserSessionService _userSessionService;

    public OrcidController(ConfluxContext context, IVariantFeatureManager featureManager,
        IUserSessionService userSessionService, IConfiguration configuration, IPeopleService peopleService,
        IPersonRetrievalService? personRetrievalService = null)
    {
        _context = context;
        _userSessionService = userSessionService;
        _featureManager = featureManager;
        _personRetrievalService = personRetrievalService!;
        _peopleService = peopleService;
        // Ensure configuration key matches exactly, including case if necessary
        // Use Value property first, which allows for better testing with mocks
        IConfigurationSection redirectsSection = configuration.GetSection("Authentication:Orcid:AllowedRedirectUris");
        string? redirectValue = redirectsSection.Value;
        if (redirectValue != null)
            _allowedRedirects = redirectValue.Split(',');
        else
            // Fall back to extension method if Value is null
            _allowedRedirects = redirectsSection.Get<string[]>() ?? [];
    }

    [HttpGet("link")]
    public async Task<IActionResult> LinkOrcid([FromQuery] string redirectUri)
    {
        // Validate redirect URL
        string safeRedirectUri =
            IsValidRedirectUrl(redirectUri) ? redirectUri : "/orcid/redirect"; // Consider a safer default or error

        if (await _featureManager.IsEnabledAsync("OrcidAuthentication"))
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = "/orcid/finalize",
                Items =
                {
                    { "finalRedirect", safeRedirectUri },
                }, // Pass the *original* desired redirect URI
            }, "orcid");

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
        const string exampleOrcid = "0000-0002-1825-0097";

        // If SRAM is enabled (but OrcidAuthentication feature flag is off), update DB with hardcoded ORCID.
        // This still needs the fix to avoid the DbUpdateException.
        User? dbUser = await _context.Users.FindAsync(userSession.User.Id);
        if (dbUser == null)
            return NotFound("User not found in database.");

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
        if (url.StartsWith('/')) return true;

        // Check if the URL is absolute and in the allowed redirect URLs list
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            // Check against configured allowed origins/redirects
            return _allowedRedirects.Any(allowed =>
                uri.OriginalString.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));

        return false;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(List<Person>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Person>>> GetPersonByQuery([FromQuery] string? query)
    {
        if (!await _featureManager.IsEnabledAsync("OrcidIntegration"))
            return BadRequest("ORCID integration is not enabled.");

        List<OrcidPerson> orcidPeople = await _personRetrievalService!.FindPeopleByNameFast(query);
        if (orcidPeople.Count == 0)
            return Ok(new List<Person>());

        List<Person> people = new();
        foreach (OrcidPerson orcidPerson in orcidPeople)
        {
            PersonRequestDTO personDTO = new()
            {
                Name = orcidPerson.CreditName ?? orcidPerson.FirstName + " " + orcidPerson.LastName,
                GivenName = orcidPerson.FirstName,
                FamilyName = orcidPerson.LastName,
                Email = null,
                ORCiD = orcidPerson.Orcid,
            };
            if (orcidPerson.Orcid == null)
                continue; // Skip if ORCID is null

            Person? person = await _peopleService.GetPersonByOrcidIdAsync(orcidPerson.Orcid);
            // Check if user with ORCID already exists
            if (person is not null)
            {
                people.Add(person);
                continue; // Skip if user already exists
            }

            person = await _peopleService.CreatePersonAsync(personDTO);
            people.Add(person);
        }


        return people;
    }

    [HttpGet("people")]
    [ProducesResponseType(typeof(Person), StatusCodes.Status200OK)]
    public async Task<ActionResult<Person>> GetPersonFromOrcid([FromQuery] string orcid)
    {
        if (!await _featureManager.IsEnabledAsync("OrcidIntegration"))
            return BadRequest("ORCID integration is not enabled.");

        // Check if user with orcid already exists
        Person? person = await _peopleService.GetPersonByOrcidIdAsync(orcid);
        if (person is not null)
            return person;

        OrcidPerson orcidPerson;
        try
        {
            orcidPerson = await _personRetrievalService!.FindPersonByOrcid(orcid);
        }
        catch (OrcidServiceException ex)
        {
            return BadRequest($"Error retrieving ORCID data: {ex.Message}");
        }

        PersonRequestDTO newPersonDTO = new()
        {
            Name = orcidPerson.CreditName ?? orcidPerson.FirstName + " " + orcidPerson.LastName,
            GivenName = orcidPerson.FirstName,
            FamilyName = orcidPerson.LastName,
            Email = null,
            ORCiD = orcidPerson.Orcid,
        };

        Person newPerson = await _peopleService.CreatePersonAsync(newPersonDTO);
        return newPerson;
    }

    [HttpGet("finalize")]
    public async Task<IActionResult> OrcidFinalize([FromQuery] string redirectUri)
    {
        // Validate the effective redirect URL
        string safeRedirectUri = IsValidRedirectUrl(redirectUri) ? redirectUri : "/orcid/redirect";

        AuthenticateResult authenticateResult = await HttpContext.AuthenticateAsync("OrcidCookie");
        string? orcidId = authenticateResult.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(orcidId))
        {
            // Fallback to session if claim is missing (it is almost always and I don't know why)
            orcidId = HttpContext.Session.GetString("orcid");
            if (string.IsNullOrEmpty(orcidId))
                return BadRequest("Could not retrieve ORCID ID from claims or session.");
        }

        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession?.User == null)
            // This scenario might happen if the primary session expired while the user was at ORCID.
            // Handle appropriately - maybe redirect to login?
            return Unauthorized("Primary user session not found or invalid. Please log in again.");

        User? dbUser = await _context.Users.FindAsync(userSession.User.Id);
        if (dbUser == null)
            // User exists in session but not DB? This indicates an inconsistency.
            return NotFound($"User with ID {userSession.User.Id} not found in database.");

        dbUser.ORCiD = orcidId;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Log the detailed exception
            Console.WriteLine($"Error saving ORCID link: {ex}");
            // Provide a generic error to the client
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while linking the ORCID.");
        }

        await _userSessionService.UpdateUser();
        return Redirect(safeRedirectUri);
    }

    [HttpGet("unlink")]
    public async Task<IActionResult> OrcidUnlink()
    {
        // Get current user session
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession?.User == null)
            return Unauthorized("User not logged in or session invalid.");

        // Fetch the corresponding User entity from the database
        User? dbUser = await _context.Users.FindAsync(userSession.User.Id);
        if (dbUser == null) return NotFound($"User with ID {userSession.User.Id} not found in database.");

        // Update *only* the ORCiD property
        dbUser.ORCiD = null;

        // Save changes
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Error saving ORCID unlink: {ex}");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while unlinking the ORCID.");
        }

        userSession.User = dbUser;
        await _userSessionService.CommitUser(userSession);

        return Ok("ORCID successfully unlinked.");
    }

    [HttpGet("redirect")]
    public IActionResult OrcidRedirect() =>
        // This just has to be a valid endpoint. It might be where ORCID initially redirects *before*
        // the cookie middleware processes the request and redirects again to `finalize`.
        Ok("ORCID redirect endpoint reached. Processing...");
}
