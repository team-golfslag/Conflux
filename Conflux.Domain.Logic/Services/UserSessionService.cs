// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using Conflux.Domain.Models;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.Extensions;
using Microsoft.AspNetCore.Http;

namespace Conflux.Domain.Logic.Services;

public interface IUserSessionService
{
    Task<UserSession?> GetUser();
    Task<UserSession?> SetUser(ClaimsPrincipal? claims);
    void ClearUser();
}

public class UserSessionService: IUserSessionService
{
    private const string UserKey = "UserProfile";

    private readonly IHttpContextAccessor _httpContextAccessor;
   
    private readonly CollaborationMapper _collaborationMapper;

    public UserSessionService(
        IHttpContextAccessor httpContextAccessor, CollaborationMapper collaborationMapper)
    {
        _httpContextAccessor = httpContextAccessor;
        _collaborationMapper = collaborationMapper;
    }

    public async Task<UserSession?> GetUser()
    {
        if (_httpContextAccessor.HttpContext.Session is null ||
            !_httpContextAccessor.HttpContext.Session.IsAvailable) return await SetUser(null);
        UserSession? userSession = _httpContextAccessor.HttpContext?.Session.Get<UserSession>(UserKey);
        if (userSession != null)
            return userSession;

        return await SetUser(null);
    }

    public async Task<UserSession?> SetUser(ClaimsPrincipal? claims)
    {
        if (_httpContextAccessor.HttpContext?.User is null)
            throw new InvalidOperationException("User is not authenticated.");

        if (claims is null && _httpContextAccessor.HttpContext?.User.Identity is null)
            return null;

        var collaborationDTOs = claims != null ? claims.GetCollaborations() : 
            _httpContextAccessor.HttpContext?.User.GetCollaborations();
        if (collaborationDTOs is null)
            throw new InvalidOperationException("User has no collaborations.");

        var collaborations = await _collaborationMapper.Map(collaborationDTOs);
        UserSession user = new()
        {
            SRAMId = _httpContextAccessor.HttpContext?.User.GetClaimValue("personIdentifier"),
            Name = _httpContextAccessor.HttpContext?.User.GetClaimValue("Name"),
            GivenName = _httpContextAccessor.HttpContext?.User.GetClaimValue("given_name"),
            FamilyName = _httpContextAccessor.HttpContext?.User.GetClaimValue("family_name"),
            Email = _httpContextAccessor.HttpContext?.User.GetClaimValue("Email"),
            Collaborations = collaborations,
        };
        
        _httpContextAccessor.HttpContext?.Session.Set(UserKey, user);
        
        return user;
    }

    public void ClearUser()
    {
        if (_httpContextAccessor.HttpContext.Session is not null && _httpContextAccessor.HttpContext.Session.IsAvailable)
            _httpContextAccessor.HttpContext?.Session.Remove(UserKey);
    }
}
