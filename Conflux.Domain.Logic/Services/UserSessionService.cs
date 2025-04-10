// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Models;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.Extensions;
using Microsoft.AspNetCore.Http;

namespace Conflux.Domain.Logic.Services;

public interface IUserSessionService
{
    UserSession? GetUser();
    Task SetUser();
    void ClearUser();
}

public class UserSessionService: IUserSessionService
{
    private const string UserKey = "UserProfile";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ConfluxContext _context;

    private readonly SCIMApiClient _scimApiClient;
    private readonly CollaborationMapper _collaborationMapper;

    public UserSessionService(
        IHttpContextAccessor httpContextAccessor,
        ConfluxContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        
        _scimApiClient = new SCIMApiClient(new HttpClient()
        {
            BaseAddress = new Uri("https://sram.surf.nl/api/scim/v2/")
        });
        string? scimSecret = Environment.GetEnvironmentVariable("SRAM_SCIM_SECRET");
        if (string.IsNullOrEmpty(scimSecret))
            throw new InvalidOperationException("SRAM SCIM secret must be specified in environment variable.");
        _scimApiClient.SetBearerToken(scimSecret);
        
        _collaborationMapper = new CollaborationMapper(_context, _scimApiClient);
    }
    
    public UserSession? GetUser() =>
        _httpContextAccessor.HttpContext?.Session.Get<UserSession>(UserKey);

    public async Task SetUser()
    {
        if (_httpContextAccessor.HttpContext?.User is null)
            throw new InvalidOperationException("User is not authenticated.");
        
        if (GetUser() is not null)
            return;

        var collaborationDtos = _httpContextAccessor.HttpContext?.User.GetCollaborations();
        if (collaborationDtos is null)
            throw new InvalidOperationException("User has no collaborations.");

        var collaborations = await _collaborationMapper.Map(collaborationDtos);
        var user = new UserSession
        {
            SRAMId = _httpContextAccessor.HttpContext?.User.GetClaimValue("personIdentifier"),
            Name = _httpContextAccessor.HttpContext?.User.GetClaimValue("Name"),
            GivenName = _httpContextAccessor.HttpContext?.User.GetClaimValue("given_name"),
            FamilyName = _httpContextAccessor.HttpContext?.User.GetClaimValue("family_name"),
            Email = _httpContextAccessor.HttpContext?.User.GetClaimValue("Email"),
            Collaborations = collaborations
        };
        
        _httpContextAccessor.HttpContext?.Session.Set(UserKey, user);
    }

    public void ClearUser() =>
        _httpContextAccessor.HttpContext?.Session.Remove(UserKey);
}
