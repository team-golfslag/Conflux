// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
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
    private readonly IUserSessionService _userSessionService;
    public UserSessionController(IUserSessionService userSessionService) => _userSessionService = userSessionService;

    [HttpGet]
    [Route("login")]
    [Authorize]
    public async Task<ActionResult> LogIn() => Ok();
   
    
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
