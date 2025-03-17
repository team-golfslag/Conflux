using Conflux.API.DTOs;
using Conflux.Data;
using Conflux.Domain;

namespace Conflux.API.Services;

public class ProjectService
{
    private readonly ConfluxContext _context;

    public ProjectService(ConfluxContext context)
    {
        _context = context;
    }

    public async Task<Project?> UpdateProjectAsync(Guid id, ProjectUpdateDto dto)
    {
        Project? project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            return null;
        }
        
        project.Title = dto.Title ?? project.Title;
        project.Description = dto.Description ?? project.Description;
        project.StartDate = dto.StartDate ?? project.StartDate;
        project.EndDate = dto.EndDate ?? project.EndDate;
        
        await _context.SaveChangesAsync();
        return project;
    }
}
