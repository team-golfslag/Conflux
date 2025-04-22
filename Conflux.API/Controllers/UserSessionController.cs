// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.Services;
using Conflux.Domain.Models;
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
    public async Task<ActionResult> LogIn([FromQuery] string redirectUri)
    {
        UserSession? user = await _userSessionService.GetUser();
        if (user is null) return Unauthorized();

        await _sessionMappingService.CollectSessionData(user);
        await _userSessionService.UpdateUser();
        if (!IsValidRedirectUrl(redirectUri)) return new ForbidResult();

        // Validate redirect URL
        return Redirect(redirectUri);
    }

    [HttpGet]
    [Route("logout")]
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
    public async Task<ActionResult<UserSession>> UserSession() =>
        await _userSessionService.GetUser() ?? throw new InvalidOperationException();

    private bool IsValidRedirectUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        // Check if the URL is in the allowed redirect URLs list
        return _allowedRedirects.Any(allowed =>
            url.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));
    }
}
