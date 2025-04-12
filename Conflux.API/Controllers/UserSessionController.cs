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

namespace Conflux.API.Controllers;

[ApiController]
[Route("session")]
public class UserSessionController : ControllerBase
{
    private readonly SessionCollectionService _sessionCollectionService;
    private readonly IUserSessionService _userSessionService;

    public UserSessionController(IUserSessionService userSessionService,
        SessionCollectionService sessionCollectionService)
    {
        _userSessionService = userSessionService;
        _sessionCollectionService = sessionCollectionService;
    }

    [HttpGet]
    [Route("login")]
    [Authorize]
    public async Task<ActionResult> LogIn([FromQuery] string redirect)
    {
        Redirect(redirect);
        UserSession? user = await _userSessionService.GetUser();
        if (user is null) return Unauthorized();

        await _sessionCollectionService.HandleSession(user);
        return Redirect(redirect);
    }

    // Now we check if the user is present in the database
    [HttpGet]
    [Route("logout")]
    public async Task<ActionResult> LogOut([FromQuery] string redirectUri) =>
        SignOut(new AuthenticationProperties
            {
                RedirectUri = redirectUri,
            },
            OpenIdConnectDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<UserSession>> UserSession() => Ok(await _userSessionService.GetUser());
}
