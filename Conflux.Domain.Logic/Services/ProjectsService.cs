// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Session;
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
            .Select(c => c.CollaborationGroup.SCIMId)
            .ToList();

        var data = await _context.Projects
            .Where(p => p.SCIMId != null && accessibleSramIds.Contains(p.SCIMId))
            .Select(p => new
            {
                Project = p,
                p.Products,
                p.Contributors,
                p.Titles,
                Users = p.Users.Select(person => new
                {
                    Person = person,
                    Roles = person.Roles.Where(role => role.ProjectId == p.Id).ToList(),
                }),
                Parties = p.Organisations,
            })
            .ToListAsync();

        // create a list of projects with the same size as the data
        var projects = new List<Project>(data.Count);

        foreach (var project in data)
        {
            Project newProject = project.Project;
            newProject.Products = project.Products;
            newProject.Contributors = project.Contributors;
            newProject.Titles = project.Titles;
            newProject.Users = project.Users.Select(p => p.Person with
            {
                Roles = p.Roles.ToList(),
            }).ToList();
            newProject.Organisations = project.Parties;
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
    public async Task<List<UserRole>?> GetRolesFromProject(Project project)
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();
        Collaboration? collaboration =
            userSession.Collaborations.FirstOrDefault(c => c.CollaborationGroup.SCIMId == project.SCIMId);
        if (collaboration is null)
            return null;
        var roles = await _context.UserRoles
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
    /// Gets all projects whose title or description contain the query (case-insensitive),
    /// and optionally filters by start and/or end date.
    /// </summary>
    /// <param name="dto">
    /// The <see cref="ProjectQueryDTO" /> that contains the query term, filters and 'order by' method for
    /// the query
    /// </param>
    /// <returns>Filtered and ordered list of projects</returns>
    public async Task<List<Project>> GetProjectsByQueryAsync(ProjectQueryDTO dto)
    {
        IEnumerable<Project> projects = await GetAvailableProjects();

        if (!string.IsNullOrWhiteSpace(dto.Query))
        {
            string loweredQuery = dto.Query.ToLowerInvariant();
#pragma warning disable CA1862 // CultureInfo.IgnoreCase cannot by converted to a SQL query, hence we ignore this warning
            projects = projects.Where(project =>
                project.Titles.Any(t => t.Text.ToLowerInvariant().Contains(loweredQuery)) ||
                project.Descriptions.Any(t => t.Text.ToLowerInvariant().Contains(loweredQuery)));
#pragma warning restore CA1862
        }

        DateTime? startDate;
        if (dto.StartDate.HasValue)
        {
            startDate = DateTime.SpecifyKind(dto.StartDate.Value, DateTimeKind.Utc);
            projects = projects.Where(project => project.StartDate >= startDate);
        }

        DateTime? endDate;
        if (dto.EndDate.HasValue)
        {
            endDate = DateTime.SpecifyKind(dto.EndDate.Value, DateTimeKind.Utc);
            projects = projects.Where(project => project.EndDate != null && project.EndDate <= endDate);
        }

        if (dto is { StartDate: not null, EndDate: not null })
            projects = projects.Where(project => project.StartDate <= dto.EndDate && project.EndDate >= dto.StartDate);

        projects = dto.OrderByType switch
        {
            OrderByType.TitleAsc => projects.OrderBy(project =>
                project.Titles.FirstOrDefault(t => t.Type == TitleType.Primary)),
            OrderByType.TitleDesc => projects.OrderByDescending(project =>
                project.Titles.FirstOrDefault(t => t.Type == TitleType.Primary)),
            OrderByType.StartDateAsc  => projects.OrderBy(project => project.StartDate),
            OrderByType.StartDateDesc => projects.OrderByDescending(project => project.StartDate),
            OrderByType.EndDateAsc    => projects.OrderBy(project => project.EndDate),
            OrderByType.EndDateDesc   => projects.OrderByDescending(project => project.EndDate),
            _                         => projects,
        };

        return projects.ToList();
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="dto">The DTO which to convert to a <see cref="Project" /></param>
    /// <returns>The created project</returns>
    public async Task<Project> CreateProjectAsync(ProjectDTO dto)
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
    public async Task<Project> PutProjectAsync(Guid id, ProjectDTO dto)
    {
        Project project = await _context.Projects.FindAsync(id)
            ?? throw new ProjectNotFoundException(id);

        project.Titles = dto.Titles.ConvertAll(title => title.ToProjectTitle(id));
        project.Descriptions = dto.Descriptions.ConvertAll(desc => desc.ToProjectDescription(id));
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
        Project project = await _context.Projects.Include(p => p.Titles)
                .Include(p => p.Descriptions)
                .SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new ProjectNotFoundException(id);

        project.Titles = dto.Titles?.ConvertAll(t => t.ToProjectTitle(id)) ?? project.Titles;
        project.Descriptions = dto.Descriptions?.ConvertAll(d => d.ToProjectDescription(id)) ?? project.Descriptions;
        project.StartDate = dto.StartDate ?? project.StartDate;
        project.EndDate = dto.EndDate ?? project.EndDate;

        await _context.SaveChangesAsync();
        return project;
    }
}
