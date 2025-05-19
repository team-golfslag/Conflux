// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

public class AccessControlService(ConfluxContext context) : IAccessControlService
{
    public async Task<bool> UserHasRoleInProject(Guid userId, Guid projectId, UserRoleType roleType)
    {
        User? user = await context.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user != null && user.Roles.Any(r => r.ProjectId == projectId && r.Type == roleType);
    }
}
