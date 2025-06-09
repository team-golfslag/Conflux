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
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// The service for <see cref="Project" />.
/// </summary>
public class ProjectsService : IProjectsService
{
    private readonly ConfluxContext _context;
    private readonly IUserSessionService _userSessionService;

    public ProjectsService(ConfluxContext context, IUserSessionService userSessionService)
    {
        _context = context;
        _userSessionService = userSessionService;
    }

    /// <summary>
    /// Creates a base query for Projects with all related entities included.
    /// This helper method centralizes the query logic to avoid duplication.
    /// </summary>
    /// <returns>An IQueryable of Project with all includes.</returns>
    private IQueryable<Project> GetProjectsWithIncludes()
    {
        return _context.Projects
            .AsNoTracking()
            .Include(p => p.Titles)
            .Include(p => p.Descriptions)
            .Include(p => p.Users)
            .ThenInclude(user => user.Roles)
            .Include(p => p.Users)
            .ThenInclude(user => user.Person)
            .Include(p => p.Products)
            .Include(p => p.Organisations)
            .ThenInclude(o => o.Roles)
            .Include(p => p.Organisations)
            .ThenInclude(o => o.Organisation)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Person)
            .ThenInclude(p => p!.User)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Roles)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Positions);
    }

    /// <summary>
    /// Filters the roles for each user in a project to only include roles for that specific project.
    /// </summary>
    /// <param name="project">The project to filter roles for.</param>
    private static void FilterRolesForProject(Project project)
    {
        foreach (User user in project.Users)
        {
            user.Roles = user.Roles.Where(r => r.ProjectId == project.Id).ToList();
        }
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

    public async Task FavoriteProjectAsync(Guid projectId, bool favorite)
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        Project? project = await _context.Projects
            .Include(p => p.Users)
            .ThenInclude(u => u.Person)
            .SingleOrDefaultAsync(p => p.Id == projectId);
        
        if (project is null)
            throw new ProjectNotFoundException(projectId);

        if (userSession.User is null)
            return;
        
        var user = await _context.Users.FindAsync(userSession.User.Id);
        if (user is null)
            return;
        
        if (favorite && !user.FavoriteProjectIds.Contains(projectId))
            user.FavoriteProjectIds.Add(projectId);
        else if (!favorite && user.FavoriteProjectIds.Contains(projectId))
            user.FavoriteProjectIds.Remove(projectId);
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        await _userSessionService.UpdateUser();
    }

    /// <summary>
    /// Gets a project by its GUID.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <returns>The project DTO</returns>
    /// <exception cref="ProjectNotFoundException">Thrown when the project is not found</exception>
    public async Task<ProjectResponseDTO> GetProjectDTOByIdAsync(Guid id)
    {
        Project project = await GetProjectByIdAsync(id)
            ?? throw new ProjectNotFoundException(id);

        return MapToProjectDTO(project);
    }

    /// <summary>
    /// Gets a project by its GUID.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <returns>The project DTO</returns>
    /// <exception cref="ProjectNotFoundException">Thrown when the project is not found</exception>
    public async Task<Project> GetProjectByIdAsync(Guid id)
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();
        
        Project? project = await GetProjectsWithIncludes().SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new ProjectNotFoundException(id);

        FilterRolesForProject(project);

        if (userSession.User is not null)
        {
            userSession.User.RecentlyAccessedProjectIds =
                userSession.User.RecentlyAccessedProjectIds.Prepend(project.Id).Take(10).ToList();
            await _context.SaveChangesAsync();
            await _userSessionService.CommitUser(userSession);
        }

        List<string> accessibleSramIds = userSession.Collaborations
            .Select(c => c.CollaborationGroup.SCIMId)
            .ToList();
        if (!accessibleSramIds.Contains(project.SCIMId))
            throw new ProjectNotFoundException(id);

        return project;
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
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null || userSession.User is null)
            throw new UserNotAuthenticatedException();
        
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
        
        if (dto.Lectorate is not null) 
            projects = projects.Where(project => project.Lectorate == dto.Lectorate);

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
                project.Titles.FirstOrDefault(t => t.Type == TitleType.Primary && t.EndDate == null)?.Text),
            OrderByType.TitleDesc => projects.OrderByDescending(project =>
                project.Titles.FirstOrDefault(t => t.Type == TitleType.Primary && t.EndDate == null)?.Text),
            OrderByType.StartDateAsc  => projects.OrderBy(project => project.StartDate),
            OrderByType.StartDateDesc => projects.OrderByDescending(project => project.StartDate),
            OrderByType.EndDateAsc    => projects.OrderBy(project => project.EndDate),
            OrderByType.EndDateDesc   => projects.OrderByDescending(project => project.EndDate),
            // Default case = relevance, check if the project is contained in the users recently accessed projects
            _                         => projects.OrderByDescending(project =>
                userSession.User!.RecentlyAccessedProjectIds.Contains(project.Id))
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
            StartDate = p.StartDate.ToString("yyyy MMMM dd"),
            EndDate = p.EndDate?.ToString("yyyy MMMM dd") ?? string.Empty,
            OrganisationNames = string.Join("; ", p.Organisations.Select(o => o.Organisation.Name)),
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
        project.Lectorate = dto.Lectorate;
        project.LastestEdit = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload the project with all relationships
        Project? loadedProject = await GetProjectsWithIncludes().SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new ProjectNotFoundException(id);

        return MapToProjectDTO(loadedProject);
    }

    /// <summary>
    /// Retrieves all projects accessible to the current user based on their SRAM collaborations.
    /// </summary>
    /// <returns>A list of projects that the current user has access to</returns>
    /// <exception cref="UserNotAuthenticatedException">Thrown when the user is not authenticated</exception>
    private async Task<List<ProjectResponseDTO>> GetAvailableProjects()
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null || userSession.User is null)
            throw new UserNotAuthenticatedException();

        List<Project> projects;
        if (userSession.User.PermissionLevel == PermissionLevel.SuperAdmin)
        {
            projects = await GetProjectsWithIncludes().ToListAsync();
        }
        else if (userSession.User.PermissionLevel == PermissionLevel.SystemAdmin)
        {
            List<Project> allProjects = await GetProjectsWithIncludes().ToListAsync();
            projects = allProjects
                .Where(p => p.Lectorate != null && userSession.User.AssignedLectorates.Contains(p.Lectorate) ||
                    p.OwnerOrganisation != null && userSession.User.AssignedOrganisations.Contains(p.OwnerOrganisation))
                .ToList();
        }
        else
        {
            List<string> accessibleSramIds = userSession.Collaborations
                .Select(c => c.CollaborationGroup.SCIMId)
                .ToList();

            projects = await GetProjectsWithIncludes()
                .Where(p => accessibleSramIds.Contains(p.SCIMId))
                .ToListAsync();
        }

        // Filter roles per project per user for the retrieved projects
        projects.ForEach(FilterRolesForProject);

        return projects.Select(MapToProjectDTO).ToList();
    }

    /// <summary>
    /// Maps a Project entity to a ProjectDTO
    /// </summary>
    private ProjectResponseDTO MapToProjectDTO(Project project)
    {
        // Get all person IDs from contributors to fetch in one query
        List<Guid> personIds = project.Contributors.Select(c => c.PersonId).Distinct().ToList();

        // Fetch all persons in one go (to avoid N+1 query problem)
        Dictionary<Guid, Person> people = _context.People
            .Where(p => personIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        List<Guid> organisationIds =
            project.Organisations.Select(o => o.OrganisationId).Distinct().ToList();

        List<Organisation> organisations = _context.Organisations
            .Where(o => organisationIds.Contains(o.Id))
            .ToList();

        return new()
        {
            Id = project.Id,
            Titles = project.Titles.ConvertAll(t => new ProjectTitleResponseDTO
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Text = t.Text,
                Language = t.Language,
                Type = t.Type,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
            }),
            Descriptions = project.Descriptions.ConvertAll(d => new ProjectDescriptionResponseDTO
            {
                Id = d.Id,
                ProjectId = d.ProjectId,
                Text = d.Text,
                Type = d.Type,
                Language = d.Language,
            }),
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Users = project.Users.ConvertAll(u => new UserResponseDTO
            {
                Id = u.Id,
                SRAMId = u.SRAMId,
                SCIMId = u.SCIMId,
                Roles = u.Roles.Where(r => r.ProjectId == project.Id).ToList(),
                Person = u.Person != null
                    ? new PersonResponseDTO
                    {
                        Id = u.Person.Id,
                        Name = u.Person.Name,
                        GivenName = u.Person.GivenName,
                        FamilyName = u.Person.FamilyName,
                        Email = u.Person.Email,
                        ORCiD = u.Person.ORCiD,
                    }
                    : null
            }),
            Products = project.Products.ConvertAll(p => new ProductResponseDTO
            {
                Id = p.Id,
                ProjectId = p.ProjectId,
                Schema = p.Schema,
                Url = p.Url,
                Title = p.Title,
                Type = p.Type,
                Categories = p.Categories,
            }),
            Organisations = organisations.ConvertAll(o => new ProjectOrganisationResponseDTO
            {
                ProjectId = project.Id,
                Organisation = new OrganisationResponseDTO
                {
                    Id = o.Id,
                    Name = o.Name,
                    Roles = project.Organisations.FirstOrDefault(po => po.OrganisationId == o.Id)?.Roles.Select(r =>
                        new OrganisationRoleResponseDTO
                        {
                            Role = r.Role,
                            StartDate = r.StartDate,
                            EndDate = r.EndDate,
                        }
                    ).ToList() ?? throw new OrganisationNotFoundException(o.Id),
                    RORId = o.RORId,
                }
            }),
            Contributors = project.Contributors.Select(c => new ContributorResponseDTO
            {
                Person = people.TryGetValue(c.PersonId,
                    out Person? person)
                    ? new()
                    {
                        Id = person.Id,
                        ORCiD = person.ORCiD,
                        Name = person.Name,
                        GivenName = person.GivenName,
                        FamilyName = person.FamilyName,
                        Email = person.Email,
                        UserId = person.UserId,
                    }
                    : throw new PersonNotFoundException(c.PersonId),
                Roles = c.Roles.ConvertAll(r => new ContributorRoleResponseDTO
                {
                    PersonId = c.PersonId,
                    ProjectId = c.ProjectId,
                    RoleType = r.RoleType,
                }),
                Positions = c.Positions.ConvertAll(p => new ContributorPositionResponseDTO
                {
                    PersonId = c.PersonId,
                    ProjectId = c.ProjectId,
                    Position = p.Position,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                }),
                Leader = c.Leader,
                Contact = c.Contact,
                ProjectId = c.ProjectId,
            }).ToList(),
            Lectorate = project.Lectorate,
            OwnerOrganisation = project.OwnerOrganisation,
        };
    }

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

        //  header 
        csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        //  rows
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
