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
    private readonly IVariantFeatureManager _featureManager;
    private readonly SessionMappingService _sessionMappingService;
    private readonly IUserSessionService _userSessionService;

    public UserSessionController(IUserSessionService userSessionService,
        SessionMappingService sessionMappingService, IVariantFeatureManager featureManager)
    {
        _userSessionService = userSessionService;
        _sessionMappingService = sessionMappingService;
        _featureManager = featureManager;
    }

    [HttpGet]
    [Route("login")]
    [Authorize]
    public async Task<ActionResult> LogIn([FromQuery] string redirect)
    {
        UserSession? user = await _userSessionService.GetUser();

        if (user is null) return Unauthorized();

        await _sessionMappingService.CollectSessionData(user);
        await _userSessionService.UpdateUser();
        return Redirect(redirect);
    }

    // Now we check if the user is present in the database
    [HttpGet]
    [Route("logout")]
    public async Task<ActionResult> LogOut([FromQuery] string redirectUri) =>
        await _featureManager.IsEnabledAsync("SRAMAuthentication")
            ? SignOut(new AuthenticationProperties
                {
                    RedirectUri = redirectUri,
                },
                OpenIdConnectDefaults.AuthenticationScheme,
                CookieAuthenticationDefaults.AuthenticationScheme
            )
            : SignOut(new AuthenticationProperties
                {
                    RedirectUri = redirectUri,
                },
                CookieAuthenticationDefaults.AuthenticationScheme
            );

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<UserSession>> UserSession() => await _userSessionService.GetUser() ?? throw new InvalidOperationException();
}
