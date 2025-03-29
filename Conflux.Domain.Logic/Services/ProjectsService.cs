using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Exceptions;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// The service for <see cref="Project" />.
/// </summary>
public class ProjectsService
{
    private readonly ConfluxContext _context;

    public ProjectsService(ConfluxContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a project to the database.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <param name="dto">The Data Transfer Object for the project</param>
    /// <returns>The added project</returns>
    public async Task<Project> PutProjectAsync(Guid id, ProjectPutDTO dto)
    {
        Project project = await _context.Projects.FindAsync(id)
            ?? throw new ProjectNotFoundException(id);

        project.Title = dto.Title ?? project.Title;
        project.Description = dto.Description ?? project.Description;
        project.StartDate = dto.StartDate ?? project.StartDate;
        project.EndDate = dto.EndDate ?? project.EndDate;

        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<Project> PatchProjectAsync(Guid id, ProjectPatchDTO dto)
    {
        Project project = await _context.Projects.FindAsync(id)
            ?? throw new ProjectNotFoundException(id);

        project.Title = dto.Title ?? project.Title;
        project.Description = dto.Description ?? project.Description;
        project.StartDate = dto.StartDate ?? project.StartDate;
        project.EndDate = dto.EndDate ?? project.EndDate;

        await _context.SaveChangesAsync();
        return project;
    }
}
