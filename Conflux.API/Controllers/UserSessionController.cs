// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace Conflux.API.Controllers;

[ApiController]
[Route("session")]
public class UserSessionController : ControllerBase
{
    private readonly string[] _allowedRedirects;
    private readonly IVariantFeatureManager _featureManager;
    private readonly ISessionMappingService _sessionMappingService;
    private readonly IUserSessionService _userSessionService;

    public UserSessionController(
        IUserSessionService userSessionService,
        ISessionMappingService sessionMappingService,
        IVariantFeatureManager featureManager,
        IConfiguration configuration)
    {
        _userSessionService = userSessionService;
        _sessionMappingService = sessionMappingService;
        _featureManager = featureManager;
        _allowedRedirects = configuration.GetSection("Authentication:SRAM:AllowedRedirectUris").Get<string[]>() ?? [];
    }

    [HttpGet]
    [Route("login")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<ActionResult> LogIn([FromQuery] string redirectUri)
    {
        UserSession? user;
        try
        {
            user = await _userSessionService.GetSession();
        }
        catch (Exception)
        {
            // clear user claims and force re-authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            // redirect to login page
            return Redirect("login?redirectUri=" + redirectUri);
        }

        if (user is null) return Unauthorized();

        await _sessionMappingService.CollectSessionData(user);
        if (!IsValidRedirectUrl(redirectUri)) return new ForbidResult();

        // Validate redirect URL
        return Redirect(redirectUri);
    }

    [HttpGet]
    [Route("logout")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<ActionResult> LogOut([FromQuery] string redirectUri)
    {
        // Validate redirect URL
        string safeRedirectUri = IsValidRedirectUrl(redirectUri) ? redirectUri : "/";

        return await _featureManager.IsEnabledAsync("SRAMAuthentication")
            ? SignOut(new AuthenticationProperties
                {
                    RedirectUri = safeRedirectUri,
                },
                OpenIdConnectDefaults.AuthenticationScheme,
                CookieAuthenticationDefaults.AuthenticationScheme
            )
            : SignOut(new AuthenticationProperties
                {
                    RedirectUri = safeRedirectUri,
                },
                CookieAuthenticationDefaults.AuthenticationScheme
            );
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<UserSessionResponseDTO>> UserSession()
    {
        UserSession userSession = await _userSessionService.GetSession() ?? throw new InvalidOperationException();
        User user = await _userSessionService.GetUser();
        return new UserSessionResponseDTO
        {
            SRAMId = userSession.SRAMId,
            Name = userSession.Name,
            GivenName = userSession.GivenName,
            FamilyName = userSession.FamilyName,
            Email = userSession.Email,
            User = new()
            {
                Id = user.Id,
                SRAMId = user.SRAMId,
                SCIMId = user.SCIMId,
                Roles = user.Roles,
                PermissionLevel = user.PermissionLevel,
                AssignedLectorates = user.AssignedLectorates,
                AssignedOrganisations = user.AssignedOrganisations,
                RecentlyAccessedProjectIds = user.RecentlyAccessedProjectIds,
                FavouriteProjectIds = user.FavoriteProjectIds,
                Person = user.Person != null
                    ? new PersonResponseDTO
                    {
                        Id = user.Person.Id,
                        ORCiD = user.Person.ORCiD,
                        Name = user.Person.Name,
                        GivenName = user.Person.GivenName,
                        FamilyName = user.Person.FamilyName,
                        Email = user.Person.Email,
                    }
                    : null,
            },
            Collaborations = userSession.Collaborations
        };
    }
    

    private bool IsValidRedirectUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        // Check if the URL is in the allowed redirect URLs list
        return _allowedRedirects.Any(allowed =>
            url.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));
    }
}
