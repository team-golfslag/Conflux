// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Exceptions;
using Microsoft.EntityFrameworkCore;

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
    /// Gets a project by its GUID.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <returns>The project</returns>
    /// <exception cref="ProjectNotFoundException">Thrown when the project is not found</exception>
    public async Task<Project> GetProjectByIdAsync(Guid id) =>
        await _context.Projects
            .Include(p => p.People)
            .Include(p => p.Products)
            .Include(p => p.Parties)
            .SingleOrDefaultAsync(p => p.Id == id)
        ?? throw new ProjectNotFoundException(id);

    /// <summary>
    /// Gets all projects whose title or description contains the query (case-insensitive),
    /// </summary>
    /// <param name="query">The string to search in the title or description</param>
    /// <param name="startDate">The start date of the project</param>
    /// <param name="endDate">The end date of the project</param>
    /// <returns>Filtered list of projects</returns>
    public async Task<List<Project>> GetProjectsByQueryAsync(string? query, DateTime? startDate, DateTime? endDate)
    {
        IQueryable<Project> projects = _context.Projects
            .Include(p => p.People)
            .Include(p => p.Products)
            .Include(p => p.Parties);

        if (!string.IsNullOrWhiteSpace(query))
        {
            string loweredQuery = query.ToLowerInvariant();
#pragma warning disable CA1862 // CultureInfo.IgnoreCase cannot by converted to a SQL query, hence we ignore this warning
            projects = projects.Where(project =>
                project.Title.ToLower().Contains(loweredQuery) ||
                (project.Description ?? "").ToLower().Contains(loweredQuery));
#pragma warning restore CA1862
        }

        if (startDate.HasValue)
        {
            startDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            projects = projects.Where(project => project.StartDate != null && project.StartDate >= startDate);
        }

        if (endDate.HasValue)
        {
            endDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
            projects = projects.Where(project => project.EndDate != null && project.EndDate <= endDate);
        }

        if (startDate.HasValue && endDate.HasValue)
            projects = projects.Where(project => project.StartDate <= endDate && project.EndDate >= startDate);

        return await projects.ToListAsync();
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="dto">The DTO which to convert to a <see cref="Project" /></param>
    /// <returns>The created project</returns>
    public async Task<Project> CreateProjectAsync(ProjectPostDTO dto)
    {
        Project project = dto.ToProject();
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    /// <summary>
    /// Gets all projects.
    /// </summary>
    /// <returns>All projects</returns>
    public async Task<List<Project>> GetAllProjectsAsync() =>
        await _context.Projects
            .Include(p => p.Products)
            .Include(p => p.People)
            .Include(p => p.Parties)
            .ToListAsync();

    /// <summary>
    /// Updates a project to the database via PUT.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <param name="dto">The Data Transfer Object for the project</param>
    /// <returns>The added project</returns>
    /// <exception cref="ProjectNotFoundException">Thrown when the project is not found</exception>
    public async Task<Project> PutProjectAsync(Guid id, ProjectPutDTO dto)
    {
        Project project = await _context.Projects.FindAsync(id)
            ?? throw new ProjectNotFoundException(id);

        project.Title = dto.Title;
        project.Description = dto.Description;
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;

        await _context.SaveChangesAsync();
        return project;
    }

    /// <summary>
    /// Patches a project by its GUID.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <param name="dto">The Data Transfer Object for the project</param>
    /// <returns>The patched project</returns>
    /// <exception cref="ProjectNotFoundException">Thrown when the project is not found</exception>
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
