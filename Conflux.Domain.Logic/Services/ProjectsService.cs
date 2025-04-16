// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// The service for <see cref="Project" />.
/// </summary>
public class ProjectsService
{
    private readonly ConfluxContext _context;
    private readonly IUserSessionService _userSessionService;

    public ProjectsService(ConfluxContext context, IUserSessionService userSessionService)
    {
        _context = context;
        _userSessionService = userSessionService;
    }

    /// <summary>
    /// Retrieves all projects accessible to the current user based on their SRAM collaborations.
    /// </summary>
    /// <returns>A list of projects that the current user has access to</returns>
    /// <exception cref="UserNotAuthenticatedException">Thrown when the user is not authenticated</exception>
    private async Task<List<Project>> GetAvailableProjects()
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        var accessibleSramIds = userSession.Collaborations
            .Select(c => c.CollaborationGroup.SRAMId)
            .ToList();

        var data = await _context.Projects
            .Where(p => p.SRAMId != null && accessibleSramIds.Contains(p.SRAMId))
            .Select(p => new
            {
                Project = p,
                p.Products,
                People = p.People.Select(person => new
                {
                    Person = person,
                    Roles = person.Roles.Where(role => role.ProjectId == p.Id).ToList(),
                }),
                p.Parties,
            })
            .ToListAsync();

        // create a list of projects with the same size as the data
        var projects = new List<Project>(data.Count);

        foreach (var project in data)
        {
            Project newProject = project.Project;
            newProject.Products = project.Products;
            newProject.People = project.People.Select(p => p.Person with
            {
                Roles = p.Roles.ToList(),
            }).ToList();
            newProject.Parties = project.Parties;
            projects.Add(newProject);
        }

        return projects;
    }

    /// <summary>
    /// Gets all roles for a project that the current user has access to through their SRAM collaborations.
    /// </summary>
    /// <param name="project">The project to get roles for</param>
    /// <returns>
    /// A list of roles that the user has access to through the project's SRAM connection,
    /// or null if the user doesn't have access to the project
    /// </returns>
    /// <exception cref="UserNotAuthenticatedException">Thrown when the user is not authenticated</exception>
    public async Task<List<Role>?> GetRolesFromProject(Project project)
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();
        Collaboration? collaboration =
            userSession.Collaborations.FirstOrDefault(c => c.CollaborationGroup.SRAMId == project.SRAMId);
        if (collaboration is null)
            return null;
        var roles = await _context.Roles
            .Where(r => r.ProjectId == project.Id)
            .ToListAsync();

        return roles.Where(r => collaboration.Groups.Any(g => g.Urn == r.Urn)).ToList();
    }

    /// <summary>
    /// Gets a project by its GUID.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <returns>The project</returns>
    /// <exception cref="ProjectNotFoundException">Thrown when the project is not found</exception>
    public async Task<Project> GetProjectByIdAsync(Guid id) =>
        (await GetAvailableProjects())
        .FirstOrDefault(p => p.Id == id)
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
        IEnumerable<Project> projects = await GetAvailableProjects();

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

        return projects.ToList();
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
    public async Task<List<Project>> GetAllProjectsAsync()
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();
        var projects = await GetAvailableProjects();
        return projects
            .ToList();
    }

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

    /// <summary>
    /// Updates a project by adding the person with the provided personId.
    /// </summary>
    /// <param name="projectId">The GUID of the project to update</param>
    /// <param name="personId">The GUID of the person to add to the project</param>
    /// <returns>The request response</returns>
    public async Task<Project> AddPersonToProjectAsync(Guid projectId, Guid personId)
    {
        Project project = await _context.Projects.Include(p => p.People).SingleOrDefaultAsync(p => p.Id == projectId)
            ?? throw new ProjectNotFoundException(projectId);

        Person person = await _context.People.FindAsync(personId)
            ?? throw new PersonNotFoundException(personId);

        if (project.People.Any(p => p.Id == person.Id))
            throw new PersonAlreadyAddedToProjectException(projectId, personId);

        project.People.Add(person);
        await _context.SaveChangesAsync();
        return project;
    }
}
