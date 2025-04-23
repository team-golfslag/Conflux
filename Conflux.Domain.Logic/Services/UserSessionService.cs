// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using Conflux.Data;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Extensions;
using Conflux.Domain.Session;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement;

namespace Conflux.Domain.Logic.Services;

public class UserSessionService : IUserSessionService
{
    private const string UserKey = "UserProfile";
    private readonly ICollaborationMapper _collaborationMapper;
    private readonly ConfluxContext _confluxContext;
    private readonly IVariantFeatureManager _featureManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserSessionService(
        ConfluxContext confluxContext, IHttpContextAccessor httpContextAccessor,
        ICollaborationMapper collaborationMapper,
        IVariantFeatureManager featureManager)
    {
        _confluxContext = confluxContext;
        _httpContextAccessor = httpContextAccessor;
        _collaborationMapper = collaborationMapper;
        _featureManager = featureManager;
    }

    public async Task<UserSession?> GetUser()
    {
        if (!await _featureManager.IsEnabledAsync("SRAMAuthentication"))
            return UserSession.Development();

        if (_httpContextAccessor.HttpContext.Session is null ||
            !_httpContextAccessor.HttpContext.Session.IsAvailable) return await SetUser(null);
        UserSession? userSession = _httpContextAccessor.HttpContext?.Session.Get<UserSession>(UserKey);
        if (userSession != null)
            return userSession;

        return await SetUser(null);
    }

    public async Task<UserSession?> UpdateUser()
    {
        UserSession? user = await GetUser();
        if (user is null)
            return null;

        User? person = _confluxContext.Users.SingleOrDefault(p => p.SRAMId == user.SRAMId);
        if (person is null)
            return user;

        user.User = person;
        await CommitUser(user);

        return user;
    }

    public async Task CommitUser(UserSession userSession)
    {
        if (!await _featureManager.IsEnabledAsync("SRAMAuthentication"))
            return;

        if (_httpContextAccessor.HttpContext.Session is null ||
            !_httpContextAccessor.HttpContext.Session.IsAvailable) return;

        _httpContextAccessor.HttpContext?.Session.Set(UserKey, userSession);
    }

    public async Task<UserSession?> SetUser(ClaimsPrincipal? claims)
    {
        if (!await _featureManager.IsEnabledAsync("SRAMAuthentication"))
            return UserSession.Development();
        if (_httpContextAccessor.HttpContext?.User is null)
            throw new UserNotAuthenticatedException();

        if (claims is null && _httpContextAccessor.HttpContext?.User.Identity is null)
            return null;

        var collaborationDTOs = claims != null
            ? claims.GetCollaborations()
            : _httpContextAccessor.HttpContext?.User.GetCollaborations();
        if (collaborationDTOs is null)
            throw new UserNotAuthenticatedException();

        var collaborations = await _collaborationMapper.Map(collaborationDTOs);
        UserSession user = new()
        {
            SRAMId = _httpContextAccessor.HttpContext?.User.GetClaimValue("personIdentifier")!,
            Name = _httpContextAccessor.HttpContext?.User.GetClaimValue("Name")!,
            GivenName = _httpContextAccessor.HttpContext?.User.GetClaimValue("given_name")!,
            FamilyName = _httpContextAccessor.HttpContext?.User.GetClaimValue("family_name")!,
            Email = _httpContextAccessor.HttpContext?.User.GetClaimValue("Email")!,
            Collaborations = collaborations,
        };

        _httpContextAccessor.HttpContext?.Session.Set(UserKey, user);

        return user;
    }

    public void ClearUser()
    {
        if (_httpContextAccessor.HttpContext.Session is not null &&
            _httpContextAccessor.HttpContext.Session.IsAvailable)
            _httpContextAccessor.HttpContext?.Session.Remove(UserKey);
    }
}
