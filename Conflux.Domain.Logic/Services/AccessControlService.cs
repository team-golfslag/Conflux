// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

public class AccessControlService(ConfluxContext context) : IAccessControlService
{
    public Task<bool> UserHasRoleInProject(Guid userId, Guid projectId, UserRoleType roleType) =>
        context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId &&
                u.Roles.Any(r => r.ProjectId == projectId && r.Type == roleType));
}
