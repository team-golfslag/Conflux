// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Reflection;
using System.Text;
using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Queries;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Session;
using Conflux.Integrations.RAiD;
using Microsoft.EntityFrameworkCore;
using RAiD.Net;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// The service for <see cref="Project" />.
/// </summary>
public class ProjectsService
{
    // Compiled query to get project by ID
    private static readonly Func<ConfluxContext, Guid, Task<Project?>> GetProjectByIdQuery =
        EF.CompileAsyncQuery((ConfluxContext context, Guid id) =>
            context.Projects
                .AsNoTracking()
                .Include(p => p.Titles)
                .Include(p => p.Descriptions)
                .Include(p => p.Users)
                .ThenInclude(user => user.Roles)
                .Include(p => p.Products)
                .Include(p => p.Organisations)
                .Include(p => p.Contributors)
                .ThenInclude(c => c.Roles)
                .Include(p => p.Contributors)
                .ThenInclude(c => c.Positions)
                .SingleOrDefault(p => p.Id == id));

    private readonly ConfluxContext _context;
    private readonly IProjectMapperService _projectMapperService;
    private readonly IRAiDService _raidService;
    private readonly IUserSessionService _userSessionService;

    public ProjectsService(ConfluxContext context, IUserSessionService userSessionService,
        IProjectMapperService projectMapperService, IRAiDService raidService)
    {
        _context = context;
        _userSessionService = userSessionService;
        _projectMapperService = projectMapperService;
        _raidService = raidService;
    }

    /// <summary>
    /// Retrieves all projects accessible to the current user based on their SRAM collaborations.
    /// </summary>
    /// <returns>A list of projects that the current user has access to</returns>
    /// <exception cref="UserNotAuthenticatedException">Thrown when the user is not authenticated</exception>
    private async Task<List<ProjectResponseDTO>> GetAvailableProjects()
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        List<string> accessibleSramIds = userSession.Collaborations
            .Select(c => c.CollaborationGroup.SCIMId)
            .ToList();

        List<Project> projects = await _context.Projects
            .AsNoTracking()
            .Where(p => accessibleSramIds.Contains(p.SCIMId))
            .Include(p => p.Titles)
            .Include(p => p.Descriptions)
            .Include(p => p.Users)
            .ThenInclude(user => user.Roles)
            .Include(p => p.Products)
            .Include(p => p.Organisations)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Roles)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Positions)
            .ToListAsync();

        // filter roles per project per user
        foreach (Project project in projects)
            foreach (User user in project.Users)
                user.Roles = user.Roles
                    .Where(r => r.ProjectId == project.Id)
                    .ToList();

        return projects.Select(MapToProjectDTO).ToList();
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
        List<UserRole> roles = await _context.UserRoles
            .Where(r => r.ProjectId == project.Id)
            .ToListAsync();

        return roles.Where(r => collaboration.Groups.Any(g => g.Urn == r.Urn)).ToList();
    }

    /// <summary>
    /// Gets a project by its GUID.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <returns>The project DTO</returns>
    /// <exception cref="ProjectNotFoundException">Thrown when the project is not found</exception>
    public async Task<ProjectResponseDTO> GetProjectByIdAsync(Guid id)
    {
        Project project = await GetProjectByIdQuery(_context, id)
            ?? throw new ProjectNotFoundException(id);

        // filter roles per project per user
        foreach (User user in project.Users)
            user.Roles = user.Roles
                .Where(r => r.ProjectId == project.Id)
                .ToList();
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        List<string> accessibleSramIds = userSession.Collaborations
            .Select(c => c.CollaborationGroup.SCIMId)
            .ToList();
        if (!accessibleSramIds.Contains(project.SCIMId))
            throw new ProjectNotFoundException(id);

        return MapToProjectDTO(project);
    }

    /// <summary>
    /// Gets all projects whose title or description contain the query (case-insensitive),
    /// and optionally filters by start and/or end date.
    /// </summary>
    /// <param name="dto">
    /// The <see cref="ProjectQueryDTO" /> that contains the query term, filters and 'order by' method for
    /// the query
    /// </param>
    /// <returns>Filtered and ordered list of project DTOs</returns>
    public async Task<List<ProjectResponseDTO>> GetProjectsByQueryAsync(ProjectQueryDTO dto)
    {
        IEnumerable<ProjectResponseDTO> projects = await GetAvailableProjects();

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
    /// Exports a list of <see cref="Project" />s matching the specified query criteria into a CSV format.
    /// </summary>
    /// <param name="dto">The query criteria used to filter the projects to be exported.</param>
    /// <returns>A string containing the CSV representation of the filtered projects.</returns>
    public async Task<string> ExportProjectsToCsvAsync(ProjectQueryDTO dto)
    {
        List<ProjectResponseDTO> projects = await GetProjectsByQueryAsync(dto);

        var exportData = projects.Select(p => new
        {
            p.Id,
            PrimaryTitle = p.PrimaryTitle?.Text ?? string.Empty,
            StartDate = p.StartDate.ToString("yyyy MMMM dd"),
            EndDate = p.EndDate?.ToString("yyyy MMMM dd") ?? string.Empty,
            OrganisationNames = string.Join("; ", p.Organisations.Select(o => o.Name)),
            Contributors = string.Join("; ", p.Contributors.Select(c => c.Person.Name)),
            Products = string.Join("; ", p.Products.Select(pr => pr.Title)),
        });

        return GenerateCsv(exportData);
    }

    /// <summary>
    /// Gets all projects.
    /// </summary>
    /// <returns>All projects as DTOs</returns>
    public async Task<List<ProjectResponseDTO>> GetAllProjectsAsync()
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        List<ProjectResponseDTO> projects = await GetAvailableProjects();
        return projects.ToList();
    }

    /// <summary>
    /// Updates a project to the database via PUT.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <param name="dto">The Data Transfer Object for the project</param>
    /// <returns>The updated project DTO</returns>
    /// <exception cref="ProjectNotFoundException">Thrown when the project is not found</exception>
    public async Task<ProjectResponseDTO> PutProjectAsync(Guid id, ProjectRequestDTO dto)
    {
        Project project = await _context.Projects
                .SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new ProjectNotFoundException(id);
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;
        project.LastestEdit = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload the project with all relationships
        Project loadedProject = await GetFullProjectAsync(id)
            ?? throw new ProjectNotFoundException(id);

        return MapToProjectDTO(loadedProject);
    }

    /// <summary>
    /// Maps a Project entity to a ProjectDTO
    /// </summary>
    private ProjectResponseDTO MapToProjectDTO(Project project)
    {
        // Get all person IDs from contributors to fetch in one query
        List<Guid> personIds = project.Contributors.Select(c => c.PersonId).Distinct().ToList();

        // Fetch all persons in one go (to avoid N+1 query problem)
        Dictionary<Guid, Person> persons = _context.People
            .Where(p => personIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        List<ProjectTitle> titles = project.Titles;
        List<ProjectDescription> descriptions = project.Descriptions;

        List<Guid> organisationIds =
            project.Organisations.Select(o => o.OrganisationId).Distinct().ToList();

        var orgs = _context.Organisations.ToList();
        List<Organisation> organisations = _context.Organisations
            .Where(o => organisationIds.Contains(o.Id))
            .ToList();

        return new()
        {
            Id = project.Id,
            PrimaryTitle = titles.FirstOrDefault(t => t.Type == TitleType.Primary),
            Titles = titles,
            PrimaryDescription = descriptions.FirstOrDefault(d => d.Type == DescriptionType.Primary),
            Descriptions = project.Descriptions,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Users = project.Users,
            Products = project.Products,
            Organisations = organisations.ConvertAll(o => new ProjectOrganisationResponseDTO
            {
                Roles = project.Organisations.FirstOrDefault(po => po.OrganisationId == o.Id)?.Roles ?? throw new
                    OrganisationNotFoundException(o.Id),
                RORId = o.RORId,
                Name = o.Name,
            }),
            Contributors = project.Contributors.Select(c => new ContributorResponseDTO
            {
                Person = persons.TryGetValue(c.PersonId,
                    out Person? person)
                    ? person
                    : throw new PersonNotFoundException(c.PersonId),
                Roles = c.Roles,
                Positions = c.Positions,
                Leader = c.Leader,
                Contact = c.Contact,
                ProjectId = c.ProjectId,
            }).ToList(),
        };
    }

    private Task<Project?> GetFullProjectAsync(Guid projectId) =>
        _context.Projects
            .Include(p => p.Titles)
            .Include(p => p.Organisations)
            .ThenInclude(o => o.Roles)
            .Include(p => p.Products)
            .ThenInclude(p => p.Categories)
            .Include(p => p.Descriptions)
            .Include(p => p.Users)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Roles)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Positions)
            .SingleOrDefaultAsync(p => p.Id == projectId);

    /// <summary>
    /// Generates a CSV formatted string from a collection of data objects,
    /// where each object's properties are used to populate the CSV rows and columns.
    /// </summary>
    /// <typeparam name="T">The type of objects in the data collection to be converted to CSV format.</typeparam>
    /// <param name="data">The collection of data objects to be serialized to CSV.</param>
    /// <returns>A CSV formatted string representing the data collection.</returns>
    private static string GenerateCsv<T>(IEnumerable<T> data)
    {
        StringBuilder csv = new();
        PropertyInfo[] properties = typeof(T).GetProperties();

        // Generate header 
        csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        // Generate rows
        foreach (T item in data)
        {
            IEnumerable<string> values = properties.Select(p =>
            {
                string value = p.GetValue(item)?.ToString() ?? string.Empty;
                // Escape commas and quotes in values
                if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                    value = $"\"{value.Replace("\"", "\"\"")}\"";
                return value;
            });

            csv.AppendLine(string.Join(",", values));
        }

        return csv.ToString();
    }
}
