// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Session;
using Conflux.Integrations.RAiD;
using Microsoft.EntityFrameworkCore;
using RAiD.Net;
using RAiD.Net.Domain;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// The service for <see cref="Project" />.
/// </summary>
public class ProjectsService
{
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

    /// <summary>
    /// Retrieves all projects accessible to the current user based on their SRAM collaborations.
    /// </summary>
    /// <returns>A list of projects that the current user has access to</returns>
    /// <exception cref="UserNotAuthenticatedException">Thrown when the user is not authenticated</exception>
  private async Task<List<ProjectDTO>> GetAvailableProjects()
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        var accessibleSramIds = userSession.Collaborations
            .Select(c => c.CollaborationGroup.SCIMId)
            .ToList();

        var projects = await _context.Projects
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
        var roles = await _context.UserRoles
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
    public async Task<ProjectDTO> GetProjectByIdAsync(Guid id)
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

        var accessibleSramIds = userSession.Collaborations
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
    public async Task<List<ProjectDTO>> GetProjectsByQueryAsync(ProjectQueryDTO dto)
    {
        IEnumerable<ProjectDTO> projects = await GetAvailableProjects();

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
    /// <returns>The created project DTO</returns>
    public async Task<ProjectDTO> CreateProjectAsync(ProjectDTO dto)
    {
        Project project = dto.ToProject();
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Reload the project with all relationships
        Project loadedProject = await _context.Projects
            .Include(p => p.Titles)
            .Include(p => p.Descriptions)
            .Include(p => p.Users)
            .Include(p => p.Products)
            .Include(p => p.Organisations)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Roles)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Positions)
            .SingleAsync(p => p.Id == project.Id);

        return MapToProjectDTO(loadedProject);
    }

    /// <summary>
    /// Gets all projects.
    /// </summary>
    /// <returns>All projects as DTOs</returns>
    public async Task<List<ProjectDTO>> GetAllProjectsAsync()
    {
        UserSession? userSession = await _userSessionService.GetUser();
        if (userSession is null)
            throw new UserNotAuthenticatedException();

        var projects = await GetAvailableProjects();
        return projects.ToList();
    }

    /// <summary>
    /// Updates a project to the database via PUT.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <param name="dto">The Data Transfer Object for the project</param>
    /// <returns>The updated project DTO</returns>
    /// <exception cref="ProjectNotFoundException">Thrown when the project is not found</exception>
    public async Task<ProjectDTO> PutProjectAsync(Guid id, ProjectDTO dto)
    {
        Project project = await _context.Projects
                .Include(p => p.Titles)
                .SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new ProjectNotFoundException(id);

        project.Titles = dto.Titles.ConvertAll(title => title.ToProjectTitle(id));
        project.Descriptions = dto.Descriptions.ConvertAll(desc => desc.ToProjectDescription(id));
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;
        project.LastestEdit = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload the project with all relationships
        Project loadedProject = await _context.Projects
            .Include(p => p.Titles)
            .Include(p => p.Descriptions)
            .Include(p => p.Users)
            .Include(p => p.Products)
            .Include(p => p.Organisations)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Roles)
            .Include(p => p.Contributors)
            .ThenInclude(c => c.Positions)
            .SingleAsync(p => p.Id == id);

        return MapToProjectDTO(loadedProject);
    }

    /// <summary>
    /// Patches a project by its GUID.
    /// </summary>
    /// <param name="id">The GUID of the project</param>
    /// <param name="dto">The Data Transfer Object for the project</param>
    /// <returns>The patched project DTO</returns>
    /// <exception cref="ProjectNotFoundException">Thrown when the project is not found</exception>
    public async Task<ProjectDTO> PatchProjectAsync(Guid id, ProjectPatchDTO dto)
    {
        Project project = await _context.Projects
                .Include(p => p.Titles)
                .Include(p => p.Descriptions)
                .Include(p => p.Users)
                .Include(p => p.Products)
                .Include(p => p.Organisations)
                .Include(p => p.Contributors)
                .ThenInclude(c => c.Roles)
                .Include(p => p.Contributors)
                .ThenInclude(c => c.Positions)
                .SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new ProjectNotFoundException(id);

        project.SCIMId = dto.SCIMId ?? project.SCIMId;
        project.Titles = dto.Titles?.ConvertAll(t => t.ToProjectTitle(id)) ?? project.Titles;
        project.Descriptions = dto.Descriptions?.ConvertAll(d => d.ToProjectDescription(id)) ?? project.Descriptions;
        project.StartDate = dto.StartDate ?? project.StartDate;
        project.EndDate = dto.EndDate ?? project.EndDate;
        project.Users = dto.Users?.ConvertAll(u => u.ToUser(id)) ?? project.Users;
        project.Products = dto.Products?.ConvertAll(p => p.ToProduct()) ?? project.Products;
        project.Organisations = dto.Organisations?.ConvertAll(o => o.ToOrganisation()) ?? project.Organisations;
        project.Contributors = dto.Contributors?.ConvertAll(c => c.ToContributor()) ?? project.Contributors;
        project.LastestEdit = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToProjectDTO(project);
    }

    public async Task MintProjectInRaidAsync(Guid id)
    {
        // First map the project to the RAiDCreateProjectDTO
        Project project = await _context.Projects
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
            .SingleOrDefaultAsync(p => p.Id == id) ?? throw new ProjectNotFoundException(id);

        RAiDCreateRequest request = _projectMapperService.MapProjectCreationRequest(project);
        RAiDDto dto = await _raidService.MintRaidAsync(request) ??
            throw new RAiDException("Failed to mint project in RAiD");

        // TODO: Finish this
    }

    /// <summary>
    /// Maps a Project entity to a ProjectDTO
    /// </summary>
    private ProjectDTO MapToProjectDTO(Project project)
    {
        // Get all person IDs from contributors to fetch in one query
        var personIds = project.Contributors.Select(c => c.PersonId).Distinct().ToList();

        // Fetch all persons in one go (to avoid N+1 query problem)
        var persons = _context.People
            .Where(p => personIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var titles = project.Titles.Select(t => new ProjectTitleDTO
        {
            Text = t.Text,
            Type = t.Type,
            StartDate = t.StartDate,
            EndDate = t.EndDate,
        }).ToList();
        var descriptions = project.Descriptions.Select(d => new ProjectDescriptionDTO
        {
            Text = d.Text,
            Type = d.Type,
            Language = d.Language,
        }).ToList();
        return new()
        {
            Id = project.Id,

            PrimaryTitle = titles.FirstOrDefault(t => t.Type == TitleType.Primary),
            Titles = titles,
            PrimaryDescription = descriptions.FirstOrDefault(d => d.Type == DescriptionType.Primary),
            Descriptions = project.Descriptions.Select(d => new ProjectDescriptionDTO
            {
                Text = d.Text,
                Type = d.Type,
                Language = d.Language,
            }).ToList(),

            StartDate = project.StartDate,
            EndDate = project.EndDate,

            Users = project.Users.Select(u => new UserDTO
            {
                SRAMId = u.SRAMId,
                Name = u.Name,
                Email = u.Email,
                ORCiD = u.ORCiD,
                Roles = u.Roles.Select(r => new UserRoleDTO
                    {
                        Type = r.Type,
                        Urn = r.Urn,
                        SCIMId = r.SCIMId,
                    })
                    .ToList(),
                GivenName = u.GivenName,
                FamilyName = u.FamilyName,
                SCIMId = u.SCIMId,
            }).ToList(),

            Products = project.Products.Select(p => new ProductDTO
            {
                Title = p.Title,
                Url = p.Url,
                Categories = p.Categories.Select(c => c.Type).ToList(),
                Type = p.Type,
            }).ToList(),

            Organisations = project.Organisations.Select(o => new OrganisationDTO
            {
                Id = o.Id,
                Name = o.Name,
                RORId = o.RORId,
                Roles = o.Roles.Select(r => new OrganisationRoleDTO
                {
                    Role = r.Role,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                }).ToList(),
            }).ToList(),

            Contributors = project.Contributors.Select(c => new ContributorDTO
            {
                Person = persons.TryGetValue(c.PersonId,
                    out Person? person)
                    ? person
                    : null,
                Roles = c.Roles.Select(r => r.RoleType)
                    .ToList(),
                Positions = c.Positions.Select(p => new ContributorPositionDTO
                    {
                        Type = p.Position,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                    })
                    .ToList(),
                Leader = c.Leader,
                Contact = c.Contact,
                ProjectId = c.ProjectId,
            }).ToList(),
        };
    }
}
