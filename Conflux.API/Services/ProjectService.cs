using Conflux.API.DTOs;
using Conflux.API.Results;
using Conflux.Data;
using Conflux.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Conflux.API.Services;

/// <summary>
/// Represents the project service
/// </summary>
public class ProjectService
{
    private readonly ConfluxContext _context;

    public ProjectService(ConfluxContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Gets a project by its GUID.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <returns>The requested project</returns>
    public async Task<Project?> GetProjectByIdAsync(Guid id)
    {
        Project? project = await _context.Projects
            .Include(p => p.People)
            .Include(p => p.Products)
            .Include(p => p.Parties)
            .FirstOrDefaultAsync(p => p.Id == id);
        return project;
    }
    
    /// <summary>
    /// Updates a project by its GUID
    /// </summary>
    /// <param name="id">The GUID of the project to update<</param>
    /// <param name="dto">The new project details</param>
    /// <returns>The updated project</returns>
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
    
    /// <summary>
    /// Updates a project by adding the person with the provided personId.
    /// </summary>
    /// <param name="projectId">The GUID of the project to update</param>
    /// <param name="personId">The GUID of the person to add to the project</param>
    /// <returns>The request response</returns>
    public async Task<ProjectResult> AddPersonToProjectAsync(Guid projectId, Guid personId)
    {
        Project? project = await _context.Projects.Include(p => p.People).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null) return new (ProjectResultType.ProjectNotFound, null);
        
        Person? person = _context.People.FirstOrDefault(p => p.Id == personId);
        if (person == null) return new(ProjectResultType.PersonNotFound, project);
        if (project.People.Any(p => p.Id == person.Id)) return new (ProjectResultType.PersonAlreadyAdded, project);
        
        project.People.Add(person);
        await _context.SaveChangesAsync();
        return new (ProjectResultType.Success, project);
    }
}
