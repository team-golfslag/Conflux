// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Conflux.Domain.Logic.Services;

public class AdminService : IAdminService
{
    private readonly ConfluxContext _context;
    private readonly IConfiguration _configuration;
    private readonly IUserSessionService _userSessionService;

    public AdminService(ConfluxContext context, IConfiguration configuration, IUserSessionService userSessionService)
    {
        _context = context;
        _configuration = configuration;
        _userSessionService = userSessionService;
    }

    private UserResponseDTO MapUserToResponse(User user)
    {
        return new UserResponseDTO
        {
            Id = user.Id,
            SRAMId = user.SRAMId,
            SCIMId = user.SCIMId,
            Roles = user.Roles.ToList(),
            PermissionLevel = user.PermissionLevel,
            AssignedLectorates = user.AssignedLectorates,
            AssignedOrganisations = user.AssignedOrganisations,
            Person = new PersonResponseDTO
            {
                Id = user.PersonId,
                ORCiD = user.Person!.ORCiD,
                Name = user.Person.Name,
                GivenName = user.Person.GivenName,
                FamilyName = user.Person.FamilyName,
                Email = user.Person.Email,
                UserId = user.Person.UserId
            }
        };
    }

    /// <inheritdoc />
    public async Task<List<UserResponseDTO>> GetUsersByQuery(string? query, bool adminsOnly)
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        if (userSession.User is null || userSession.User.PermissionLevel != PermissionLevel.SuperAdmin)
            throw new UnauthorizedAccessException("You do not have permission to access this resource.");

        List<User> users = await _context.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Where(u => string.IsNullOrEmpty(query) || u.Person!.Name.Contains(query) ||
                u.Person.Email!.Contains(query))
            .Where(u => !adminsOnly || u.PermissionLevel == PermissionLevel.SystemAdmin ||
                u.PermissionLevel == PermissionLevel.SuperAdmin)
            .ToListAsync();

        return users.Select(MapUserToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<UserResponseDTO> SetUserPermissionLevel(Guid userId, PermissionLevel permissionLevel)
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        if (userSession.User is null || userSession.User.PermissionLevel != PermissionLevel.SuperAdmin)
            throw new UnauthorizedAccessException("You do not have permission to access this resource.");
        
        if (permissionLevel == PermissionLevel.SuperAdmin)
            throw new ArgumentException("Cannot set user permission level to SuperAdmin. This has to be done manually.");

        User? user = await _context.Users
            .Include(u => u.Person)
            .SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            throw new Exception($"User with ID {userId} not found.");

        user.PermissionLevel = permissionLevel;
        await _context.SaveChangesAsync();

        return MapUserToResponse(user);
    }

    /// <inheritdoc />
    public Task<List<string>> GetAvailableLectorates() =>
        Task.FromResult(_configuration.GetSection("Lectorates").Get<List<string>>() ?? []);

    /// <inheritdoc />
    public async Task<List<string>> GetAvailableOrganisations()
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();
        if (userSession.User is null || userSession.User.PermissionLevel != PermissionLevel.SuperAdmin)
            throw new UnauthorizedAccessException("You do not have permission to access this resource.");

        // get all projects with organisation set
        return await _context.Projects
            .AsNoTracking()
            .Select(p => p.OwnerOrganisation!)
            .Where(o => !string.IsNullOrEmpty(o))
            .Distinct()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<UserResponseDTO> AssignLectoratesToUser(Guid userId, List<string> lectorates)
    {
        UserSession? userSession = _userSessionService.GetUser().Result;
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        if (userSession.User is null || userSession.User.PermissionLevel != PermissionLevel.SuperAdmin)
            throw new UnauthorizedAccessException("You do not have permission to access this resource.");

        User? user = _context.Users.
            Include(u => u.Person)
            .SingleOrDefault(u => u.Id == userId);
        if (user is null)
            throw new Exception($"User with ID {userId} not found.");

        user.AssignedLectorates = lectorates;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return MapUserToResponse(user);
    }

    /// <inheritdoc />
    public async Task<UserResponseDTO> AssignOrganisationsToUser(Guid userId, List<string> organisations)
    {
        UserSession? userSession = _userSessionService.GetUser().Result;
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        if (userSession.User is null || userSession.User.PermissionLevel != PermissionLevel.SuperAdmin)
            throw new UnauthorizedAccessException("You do not have permission to access this resource.");

        User? user = _context.Users.
            Include(u => u.Person)
            .SingleOrDefault(u => u.Id == userId);
        if (user is null)
            throw new Exception($"User with ID {userId} not found.");

        user.AssignedOrganisations = organisations;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return MapUserToResponse(user);
    }
}
