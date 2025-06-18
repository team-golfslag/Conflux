// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Security.Claims;
using Conflux.Data;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Extensions;
using Conflux.Domain.Session;
using Conflux.Integrations.SRAM;
using Conflux.Integrations.SRAM.DTOs;
using Conflux.Integrations.SRAM.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace Conflux.Domain.Logic.Services;

public class UserSessionService : IUserSessionService
{
    private const string UserKey = "UserProfile";
    private readonly ICollaborationMapper _collaborationMapper;
    private readonly ConfluxContext _confluxContext;
    private readonly IVariantFeatureManager _featureManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly List<string> _superAdminEmails = [];

    public UserSessionService(
        ConfluxContext confluxContext, IHttpContextAccessor httpContextAccessor,
        ICollaborationMapper collaborationMapper,
        IVariantFeatureManager featureManager, IConfiguration configuration)
    {
        _confluxContext = confluxContext;
        _httpContextAccessor = httpContextAccessor;
        _collaborationMapper = collaborationMapper;
        _featureManager = featureManager;
        _superAdminEmails = configuration
            .GetSection("SuperAdminEmails")
            .Get<List<string>>() ?? [];
    }
    
    public async Task<User> GetUser()
    {
        UserSession? userSession = await GetSession();
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        User? user = await _confluxContext.Users
            .AsNoTracking()
            .Include(p => p.Person)
            .SingleOrDefaultAsync(p => p.Id == userSession.UserId);
        if (user is null)
            throw new UserNotAuthenticatedException();
        
        return user;
    }
    
    public async Task<UserSession?> GetSession()
    {
        // if there is no http context, we are in a test
        if (_httpContextAccessor.HttpContext == null || !await _featureManager.IsEnabledAsync("SRAMAuthentication"))
            return UserSession.Development().Item1;

        if (_httpContextAccessor.HttpContext.Session is null ||
            !_httpContextAccessor.HttpContext.Session.IsAvailable) 
            return null;
            
        UserSession? userSession = _httpContextAccessor.HttpContext?.Session.Get<UserSession>(UserKey);
        if (userSession == null)
            return null;

        User? user = await _confluxContext.Users
            .SingleOrDefaultAsync(p => p.Id == userSession.UserId);
        if (user == null || user.PermissionLevel == PermissionLevel.SuperAdmin ||
            !_superAdminEmails.Contains(userSession.Email)) return userSession;
        
        user.PermissionLevel = PermissionLevel.SuperAdmin;
        await _confluxContext.SaveChangesAsync();
        
        // Update the session with the changes
        user.PermissionLevel = PermissionLevel.SuperAdmin;
        _httpContextAccessor.HttpContext?.Session.Set(UserKey, userSession);

        return userSession;
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
        {
            (UserSession devSession, User _) = UserSession.Development();
            _httpContextAccessor.HttpContext?.Session.Set(UserKey, devSession);
            return devSession;
        }

        if (_httpContextAccessor.HttpContext?.User is null)
            throw new UserNotAuthenticatedException();


        UserSession? userSession = await GetUserSession(claims);
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        // Check if the user is already in the session
        if (userSession.UserId == Guid.Empty)
            return userSession;
        
        User user = await GetUser();

        if (user is not { PermissionLevel: PermissionLevel.User } ||
            !_superAdminEmails.Contains(userSession.Email)) return userSession;
        
        user.PermissionLevel = PermissionLevel.SuperAdmin;
        _confluxContext.Users.Update(user);
        await _confluxContext.SaveChangesAsync();

        return userSession;
    }

    public void ClearUser()
    {
        if (_httpContextAccessor.HttpContext.Session is not null &&
            _httpContextAccessor.HttpContext.Session.IsAvailable)
            _httpContextAccessor.HttpContext?.Session.Remove(UserKey);
    }

    public async Task<UserSession?> GetUserSession(ClaimsPrincipal? claims)
    {
        if (claims is null && _httpContextAccessor.HttpContext?.User.Identity is null)
            return null;

        List<CollaborationDTO>? collaborationDTOs = claims != null
            ? claims.GetCollaborations()
            : _httpContextAccessor.HttpContext?.User.GetCollaborations();
        if (collaborationDTOs is null)
            throw new UserNotAuthenticatedException();

        List<Collaboration> collaborations = await _collaborationMapper.Map(collaborationDTOs);
        return new()
        {
            SRAMId = _httpContextAccessor.HttpContext?.User.GetClaimValue("personIdentifier")!,
            Name = _httpContextAccessor.HttpContext?.User.GetClaimValue("Name")!,
            GivenName = _httpContextAccessor.HttpContext?.User.GetClaimValue("given_name")!,
            FamilyName = _httpContextAccessor.HttpContext?.User.GetClaimValue("family_name")!,
            Email = _httpContextAccessor.HttpContext?.User.GetClaimValue("Email")!,
            Collaborations = collaborations,
        };
    }
}
