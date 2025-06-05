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
            .Include(u => u.Roles)
            .SingleOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        if (user.Tier == UserTier.SuperAdmin) return true;
        
        Project? project = await context.Projects.FindAsync(projectId);
        if (project == null) return false;
        
        if (user.Tier == UserTier.SystemAdmin)
        {
            if (project.Lectorate != null && user.AssignedLectorates.Contains(project.Lectorate)) 
                return true;
            
            if (project.OwnerOrganisation != null && user.AssignedOrganisations.Contains(project.OwnerOrganisation)) 
                return true;
        }

        return user.Roles
            .Any(r => r.ProjectId == projectId && r.Type == roleType);
    }
        
}
