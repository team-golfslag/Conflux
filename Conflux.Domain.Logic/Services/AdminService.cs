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

    public AdminService(ConfluxContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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
        List<User> users = await _context.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Where(u => string.IsNullOrEmpty(query) || 
                u.Person!.Name.ToLower().Contains(query.ToLower()) ||
                u.Person.Email!.ToLower().Contains(query.ToLower()))
            .Where(u => !adminsOnly || u.PermissionLevel == PermissionLevel.SystemAdmin ||
                u.PermissionLevel == PermissionLevel.SuperAdmin)
            .ToListAsync();

        return users.Select(MapUserToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<UserResponseDTO> SetUserPermissionLevel(Guid userId, PermissionLevel permissionLevel)
    {
        User user = await _context.Users
            .Include(u => u.Person)
            .SingleOrDefaultAsync(u => u.Id == userId) ?? throw new UserNotFoundException(userId);
        
        user.PermissionLevel = permissionLevel;
        await _context.SaveChangesAsync();

        return MapUserToResponse(user);
    }

    /// <inheritdoc />
    public Task<List<string>> GetAvailableLectorates() =>
        Task.FromResult(_configuration.GetSection("Lectorates").Get<List<string>>() ?? []);

    /// <inheritdoc />
    public async Task<List<string>> GetAvailableOrganisations() =>
        // get all projects with organisation set
        await _context.Projects
            .AsNoTracking()
            .Select(p => p.OwnerOrganisation!)
            .Where(o => !string.IsNullOrEmpty(o))
            .Distinct()
            .ToListAsync();

    /// <inheritdoc />
    public async Task<UserResponseDTO> AssignLectoratesToUser(Guid userId, List<string> lectorates)
    {
        User user = await _context.Users
            .Include(u => u.Person)
            .SingleOrDefaultAsync(u => u.Id == userId) ?? throw new UserNotFoundException(userId);

        user.AssignedLectorates = lectorates;
        await _context.SaveChangesAsync();

        return MapUserToResponse(user);
    }

    /// <inheritdoc />
    public async Task<UserResponseDTO> AssignOrganisationsToUser(Guid userId, List<string> organisations)
    {
        User user = await _context.Users
            .Include(u => u.Person)
            .SingleOrDefaultAsync(u => u.Id == userId) ?? throw new UserNotFoundException(userId);

        user.AssignedOrganisations = organisations;
        await _context.SaveChangesAsync();

        return MapUserToResponse(user);
    }
}
