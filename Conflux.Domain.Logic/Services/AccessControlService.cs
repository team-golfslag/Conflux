// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

public class AccessControlService : IAccessControlService
{
    private readonly ConfluxContext _context;

    public AccessControlService(ConfluxContext context)
    {
        _context = context;
    }

    public async Task<bool> UserHasRoleInProject(Guid userId, Guid projectId, UserRoleType roleType)
    {
        var user = await _context.Users.AsNoTracking().Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return false;
        }

        return user.Roles.Any(r => r.ProjectId == projectId && r.Type == roleType);
    }
}
