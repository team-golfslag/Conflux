// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Globalization;
using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Queries;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Extensions;
using Conflux.Domain.Session;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// The service for <see cref="Project" />.
/// </summary>
public class ProjectsService : IProjectsService
{
    // Hybrid search configuration constants
    private const double TitleWeight = 0.75;       // Weight for title matches in text scoring
    private const double DescriptionWeight = 0.25; // Weight for description matches in text scoring
    private const double PerfectMatchBoost = 0.8;  // Score boost for perfect text matches
    private const double TextMatchWeight = 0.6;    // Weight for text matching in hybrid score
    private const double SemanticWeight = 0.4;     // Weight for semantic similarity in hybrid score
    private const double NoMatchPenalty = 0.5;     // Penalty when no exact matches found

    private readonly ConfluxContext _context;
    private readonly IUserSessionService _userSessionService;
    private readonly IEmbeddingService? _embeddingService;
    private readonly ILogger<ProjectsService> _logger;
    private readonly IVariantFeatureManager _featureManager;

    public ProjectsService(
        ConfluxContext context,
        IUserSessionService userSessionService,
        ILogger<ProjectsService> logger,
        IVariantFeatureManager featureManager,
        IEmbeddingService? embeddingService = null)
    {
        _context = context;
        _userSessionService = userSessionService;
        _logger = logger;
        _featureManager = featureManager;
        _embeddingService = embeddingService;
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

        User? user = await _context.Users.FindAsync(userSession.User.Id);
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
        if (userSession?.User is null)
            throw new UserNotAuthenticatedException();

        Project? project = await GetProjectsWithIncludes().SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new ProjectNotFoundException(id);

        FilterRolesForProject(project);


        userSession.User.RecentlyAccessedProjectIds =
            userSession.User.RecentlyAccessedProjectIds.Prepend(project.Id).Take(10).ToList();
        await _context.SaveChangesAsync();
        await _userSessionService.CommitUser(userSession);

        switch (userSession.User.PermissionLevel)
        {
            case PermissionLevel.SuperAdmin:
            case PermissionLevel.SystemAdmin when (
                project.Lectorate is not null && userSession.User.AssignedLectorates.Contains(project.Lectorate) ||
                project.OwnerOrganisation is not null &&
                userSession.User.AssignedOrganisations.Contains(project.OwnerOrganisation)):
            case PermissionLevel.User when (userSession.Collaborations
                .Select(c => c.CollaborationGroup.SCIMId).Contains(project.SCIMId)):
                return project;
            default:
                throw new ProjectNotFoundException(id);
        }
    }

    /// <summary>
    /// Gets all projects whose title or description contain the query (case-insensitive),
    /// and optionally filters by start and/or end date.
    /// Uses semantic search if the "SemanticSearch" feature flag is enabled and an embedding service is available,
    /// otherwise falls back to simple text search.
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

        // Check if semantic search is enabled via feature flag
        bool useSemanticSearch = await _featureManager.IsEnabledAsync("SemanticSearch");

        if (useSemanticSearch && _embeddingService != null && !string.IsNullOrWhiteSpace(dto.Query))
        {
            _logger.LogDebug("Using semantic search for query: {Query}", dto.Query);
            return await GetProjectsBySemanticSearchAsync(dto, userSession);
        }
        else
        {
            _logger.LogDebug("Using simple text search for query: {Query}", dto.Query);
            return await GetProjectsBySimpleTextSearchAsync(dto, userSession);
        }
    }

    private async Task<List<ProjectResponseDTO>> GetProjectsBySemanticSearchAsync(ProjectQueryDTO dto,
        UserSession userSession)
    {
        try
        {
            _logger.LogDebug("Performing hybrid semantic search for query: {Query}", dto.Query);

            // Generate embedding and words for the query
            Vector queryEmbedding = await _embeddingService!.GenerateEmbeddingAsync(dto.Query!);
            string[] queryWords = dto.Query!.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Get IDs of projects the user can access
            HashSet<Guid> accessibleProjectIds = await GetAccessibleProjectIdsAsync(userSession);

            // Perform a pure vector search on the database.
            var vectorResults = await _context.Projects
                .Where(p => accessibleProjectIds.Contains(p.Id) && p.Embedding != null)
                .OrderBy(p => p.Embedding!.CosineDistance(queryEmbedding))
                .Take(50) // Limit initial semantic results for performance
                .Select(p => new
                {
                    p.Id,
                    Distance = (double)p.Embedding!.CosineDistance(queryEmbedding)
                })
                .ToListAsync();

            // Convert distance to a similarity score for later use
            Dictionary<Guid, double> semanticScores = vectorResults.ToDictionary(
                r => r.Id,
                r => 1.0 - r.Distance
            );

            // Perform a separate text search to find exact matches.
            string loweredQuery = dto.Query.ToLowerInvariant();
            List<Guid> textResultIds = await _context.Projects
                .Where(p => accessibleProjectIds.Contains(p.Id) &&
                    (p.Titles.Any(t => EF.Functions.ILike(t.Text, $"%{loweredQuery}%")) ||
                        p.Descriptions.Any(d => EF.Functions.ILike(d.Text, $"%{loweredQuery}%"))))
                .Select(p => p.Id)
                .Distinct()
                .Take(50)
                .ToListAsync();

            // Combine the candidate IDs from both searches.
            List<Guid> allCandidateIds = semanticScores.Keys.Union(textResultIds).ToList();

            if (allCandidateIds.Count == 0) return [];

            // Fetch the full data for the combined candidate projects.
            List<Project> candidateProjects = await GetProjectsWithIncludes()
                .Where(p => allCandidateIds.Contains(p.Id))
                .ToListAsync();

            // Create a dictionary for fast lookups
            Dictionary<Guid, Project> projectDict = candidateProjects.ToDictionary(p => p.Id);

            // Calculate hybrid score and rank the results in memory.
            List<Project> finalScoredProjects = allCandidateIds
                .Select(id =>
                {
                    Project project = projectDict[id];
                    double semanticSimilarity = semanticScores.GetValueOrDefault(id, 0.0);
                    double textMatchScore = CalculateTextMatchScore(project, queryWords);
                    double hybridScore = CalculateHybridScoreOptimized(semanticSimilarity, textMatchScore);
                    return new
                    {
                        Project = project,
                        Score = hybridScore
                    };
                })
                .OrderByDescending(x => x.Score)
                .Select(x => x.Project)
                .ToList();

            _logger.LogInformation("Hybrid search returned {Count} results for query: {Query}",
                finalScoredProjects.Count, dto.Query);

            // Convert to DTOs and apply final filters
            List<ProjectResponseDTO> projectDtos = finalScoredProjects.Select(MapToProjectDTO).ToList();
            return ApplyFiltersAndOrdering(projectDtos, dto, userSession).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to perform semantic search for query: {Query}. Falling back to simple text search.",
                dto.Query);

            // Fallback to simple text search on any error
            return await GetProjectsBySimpleTextSearchAsync(dto, userSession);
        }
    }

    private async Task<List<ProjectResponseDTO>> GetProjectsBySimpleTextSearchAsync(ProjectQueryDTO dto,
        UserSession userSession)
    {
        _logger.LogDebug("Performing optimized simple text search for query: {Query}", dto.Query);

        // Get accessible project IDs efficiently
        HashSet<Guid> accessibleProjectIds = await GetAccessibleProjectIdsAsync(userSession);

        // Build the base query with access filters
        IQueryable<Project> baseQuery = GetProjectsWithIncludes()
            .Where(p => accessibleProjectIds.Contains(p.Id));

        // Apply text filtering at database level if query is provided
        if (!string.IsNullOrWhiteSpace(dto.Query))
        {
            string loweredQuery = dto.Query.ToLowerInvariant();

            // Use different approaches based on the database provider
            bool isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

            if (isInMemory)
                // InMemory provider doesn't support ILike, use Contains instead
                baseQuery = baseQuery.Where(p =>
                    p.Titles.Any(t => t.Text.ToLower().Contains(loweredQuery)) ||
                    p.Descriptions.Any(d => d.Text.ToLower().Contains(loweredQuery)));
            else
                // Use PostgreSQL ILike for better performance with GIN indexes
                baseQuery = baseQuery.Where(p =>
                    p.Titles.Any(t => NpgsqlDbFunctionsExtensions.ILike(EF.Functions, t.Text, $"%{loweredQuery}%")) ||
                    p.Descriptions.Any(d =>
                        NpgsqlDbFunctionsExtensions.ILike(EF.Functions, d.Text, $"%{loweredQuery}%")));
        }

        // Execute the query and load projects
        List<Project> projects = await baseQuery.ToListAsync();

        // Filter roles per project per user for the retrieved projects
        projects.ForEach(FilterRolesForProject);

        // Convert to DTOs
        List<ProjectResponseDTO> projectDtos = projects.Select(MapToProjectDTO).ToList();

        _logger.LogInformation("Simple text search returned {Count} results for query: {Query}",
            projectDtos.Count, dto.Query);

        return ApplyFiltersAndOrdering(projectDtos, dto, userSession).ToList();
    }

    private static IEnumerable<ProjectResponseDTO> ApplyFiltersAndOrdering(
        IEnumerable<ProjectResponseDTO> projects,
        ProjectQueryDTO dto, UserSession userSession)
    {
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

        // If the order type is default, we rely on the hybrid score ranking.
        // Otherwise, we apply the requested ordering.
        if (dto.OrderByType.HasValue)
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
                _ => projects.OrderByDescending(project =>
                    userSession.User!.RecentlyAccessedProjectIds.Contains(project.Id))
            };

        return projects;
    }

    /// <summary>
    /// Exports a list of <see cref="Project" />s matching the specified query criteria into a CSV format.
    /// </summary>
    /// <param name="dto">The query criteria used to filter the projects to be exported.</param>
    /// <returns>A string containing the CSV representation of the filtered projects.</returns>
    public async Task<string> ExportProjectsToCsvAsync(ProjectCsvRequestDTO dto)
    {
        List<ProjectResponseDTO> projects = await GetProjectsByQueryAsync(dto);

        // 2) Build CSV
        await using StringWriter writer = new();
        await using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);

        // 2a) Header row
        List<string> headers = ["id"];
        if (dto.IncludeStartDate) headers.Add("start_date");
        if (dto.IncludeEndDate) headers.Add("end_date");
        if (dto.IncludeUsers) headers.Add("users");
        if (dto.IncludeContributors) headers.Add("contributors");
        if (dto.IncludeProducts) headers.Add("products");
        if (dto.IncludeOrganisations) headers.Add("organisations");
        if (dto.IncludeTitle) headers.Add("titles");
        if (dto.IncludeDescription) headers.Add("descriptions");
        if (dto.IncludeLectorate) headers.Add("lectorate");
        if (dto.IncludeOwnerOrganisation) headers.Add("owner_organisation");

        foreach (string h in headers)
            csv.WriteField(h);
        await csv.NextRecordAsync();

        // 2b) Data rows
        foreach (ProjectResponseDTO p in projects)
        {
            // always write Id
            csv.WriteField(p.Id);

            if (dto.IncludeStartDate) csv.WriteField(p.StartDate);
            if (dto.IncludeEndDate) csv.WriteField(p.EndDate);
            if (dto.IncludeUsers)
                csv.WriteField(string.Join(";", p.Users.Select(u => u.Person?.Name)));
            if (dto.IncludeContributors)
                csv.WriteField(string.Join(";", p.Contributors.Select(c => c.Person.Name)));
            if (dto.IncludeProducts)
                csv.WriteField(string.Join(";", p.Products.Select(x => x.Title)));
            if (dto.IncludeOrganisations)
                csv.WriteField(string.Join(";", p.Organisations.Select(o => o.Organisation.Name)));
            if (dto.IncludeTitle)
            {
                List<ProjectTitleResponseDTO> primaryTitles = p.Titles.Where(t => t.Type == TitleType.Primary).ToList();
                ProjectTitleResponseDTO? unendedTitle = primaryTitles.FirstOrDefault(t => t.EndDate == null);
                csv.WriteField(unendedTitle is not null
                    ? unendedTitle.Text
                    : primaryTitles.OrderByDescending(t => t.EndDate).FirstOrDefault()?.Text);
            }

            if (dto.IncludeDescription)
                csv.WriteField(p.Descriptions.FirstOrDefault(d => d.Type == DescriptionType.Primary)?.Text ??
                    string.Empty);
            if (dto.IncludeLectorate) csv.WriteField(p.Lectorate);
            if (dto.IncludeOwnerOrganisation) csv.WriteField(p.OwnerOrganisation);

            await csv.NextRecordAsync();
        }

        return writer.ToString();
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

        // Use optimized approach to get accessible projects
        HashSet<Guid> accessibleProjectIds = await GetAccessibleProjectIdsAsync(userSession);

        List<Project> projects = await GetProjectsWithIncludes()
            .Where(p => accessibleProjectIds.Contains(p.Id))
            .ToListAsync();

        // Filter roles per project per user for the retrieved projects
        projects.ForEach(FilterRolesForProject);

        return projects.Select(MapToProjectDTO).ToList();
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

        // Update project embedding asynchronously if embedding service is available
        if (_embeddingService != null)
            _ = Task.Run(async () =>
            {
                try
                {
                    await UpdateProjectEmbeddingAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update embedding for project {ProjectId} after project update", id);
                }
            });

        // Reload the project with all relationships
        Project? loadedProject = await GetProjectsWithIncludes().SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new ProjectNotFoundException(id);

        return MapToProjectDTO(loadedProject);
    }

    /// <summary>
    /// Creates a base query for Projects with all related entities included.
    /// This helper method centralizes the query logic to avoid duplication.
    /// </summary>
    /// <returns>An IQueryable of Project with all includes.</returns>
    private IQueryable<Project> GetProjectsWithIncludes() =>
        _context.Projects
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

    /// <summary>
    /// Filters the roles for each user in a project to only include roles for that specific project.
    /// </summary>
    /// <param name="project">The project to filter roles for.</param>
    private static void FilterRolesForProject(Project project)
    {
        foreach (User user in project.Users) user.Roles = user.Roles.Where(r => r.ProjectId == project.Id).ToList();
    }

    /// <summary>
    /// Maps a Project entity to a ProjectDTO
    /// </summary>
    private ProjectResponseDTO MapToProjectDTO(Project project) =>
        new()
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
            Organisations = project.Organisations.Select(po => new ProjectOrganisationResponseDTO
            {
                ProjectId = project.Id,
                Organisation = new()
                {
                    Id = po.Organisation!.Id,
                    Name = po.Organisation.Name,
                    Roles = po.Roles.Select(r => new OrganisationRoleResponseDTO
                    {
                        Role = r.Role,
                        StartDate = r.StartDate,
                        EndDate = r.EndDate,
                    }).ToList(),
                    RORId = po.Organisation.RORId,
                }
            }).ToList(),
            Contributors = project.Contributors.Select(c => new ContributorResponseDTO
            {
                Person = c.Person != null
                    ? new()
                    {
                        Id = c.Person.Id,
                        ORCiD = c.Person.ORCiD,
                        Name = c.Person.Name,
                        GivenName = c.Person.GivenName,
                        FamilyName = c.Person.FamilyName,
                        Email = c.Person.Email,
                        UserId = c.Person.UserId,
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

    public async Task<int> UpdateProjectEmbeddingsAsync()
    {
        if (_embeddingService == null)
        {
            _logger.LogWarning("Embedding service not available for updating project embeddings");
            return 0;
        }

        try
        {
            // Find projects that need embedding updates
            List<Project> projectsToUpdate = await _context.Projects
                .Include(p => p.Titles)
                .Include(p => p.Descriptions)
                .Where(p => p.Embedding == null ||
                    p.EmbeddingContentHash == null ||
                    p.EmbeddingLastUpdated == null ||
                    p.EmbeddingLastUpdated < p.LastestEdit)
                .ToListAsync();

            if (projectsToUpdate.Count == 0)
            {
                _logger.LogInformation("No projects need embedding updates");
                return 0;
            }

            _logger.LogInformation("Updating embeddings for {Count} projects", projectsToUpdate.Count);

            int updatedCount = 0;
            const int batchSize = 10;

            for (int i = 0; i < projectsToUpdate.Count; i += batchSize)
            {
                List<Project> batch = projectsToUpdate.Skip(i).Take(batchSize).ToList();
                string[] texts = batch.Select(p => p.GetEmbeddingText()).ToArray();

                // Generate embeddings for the batch
                Vector[] embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);

                // Update each project
                for (int j = 0; j < batch.Count; j++)
                {
                    Project project = batch[j];
                    Vector embedding = embeddings[j];
                    string contentHash = project.GetEmbeddingContentHash();

                    // Only update if content actually changed
                    if (project.EmbeddingContentHash == contentHash)
                        continue;

                    project.Embedding = embedding;
                    project.EmbeddingContentHash = contentHash;
                    project.EmbeddingLastUpdated = DateTime.UtcNow;
                    updatedCount++;
                }

                // Save batch
                await _context.SaveChangesAsync();

                _logger.LogDebug("Updated embeddings for batch {BatchStart}-{BatchEnd}",
                    i + 1, Math.Min(i + batchSize, projectsToUpdate.Count));
            }

            _logger.LogInformation("Successfully updated embeddings for {Count} projects", updatedCount);
            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update project embeddings");
            throw;
        }
    }

    public async Task<bool> UpdateProjectEmbeddingAsync(Guid projectId)
    {
        if (_embeddingService == null)
        {
            _logger.LogWarning("Embedding service not available for updating project embedding");
            return false;
        }

        try
        {
            Project? project = await _context.Projects
                .Include(p => p.Titles)
                .Include(p => p.Descriptions)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found for embedding update", projectId);
                return false;
            }

            string currentContentHash = project.GetEmbeddingContentHash();

            // Check if embedding update is needed
            if (project.EmbeddingContentHash == currentContentHash && project.Embedding != null)
            {
                _logger.LogDebug("Project {ProjectId} embedding is already up to date", projectId);
                return false;
            }

            // Generate new embedding
            string text = project.GetEmbeddingText();
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Project {ProjectId} has no content for embedding", projectId);
                return false;
            }

            Vector embedding = await _embeddingService.GenerateEmbeddingAsync(text);

            // Update project
            project.Embedding = embedding;
            project.EmbeddingContentHash = currentContentHash;
            project.EmbeddingLastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated embedding for project {ProjectId}", projectId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update embedding for project {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// Calculates a text match score based on exact word matches in titles and descriptions.
    /// Higher scores for matches in titles, and bonus for matching multiple words.
    /// Uses whole word matching for more accurate results.
    /// </summary>
    /// <param name="project">The project to score</param>
    /// <param name="queryWords">The search terms split into individual words</param>
    /// <returns>A score between 0 and 1 representing text match quality</returns>
    private static double CalculateTextMatchScore(Project project, string[] queryWords)
    {
        if (queryWords.Length == 0) return 0;

        // Get all text content
        string allTitleText = string.Join(" ", project.Titles.Select(t => t.Text)).ToLowerInvariant();
        string allDescriptionText = string.Join(" ", project.Descriptions.Select(d => d.Text)).ToLowerInvariant();

        // Split content into words for whole word matching
        string[] titleWords = allTitleText.Split([' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?'],
            StringSplitOptions.RemoveEmptyEntries);
        string[] descriptionWords = allDescriptionText.Split([' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?'],
            StringSplitOptions.RemoveEmptyEntries);

        // Count exact word matches
        int titleWordsMatched = queryWords.Count(queryWord =>
            titleWords.Any(titleWord => titleWord.Equals(queryWord, StringComparison.OrdinalIgnoreCase)));
        int descriptionWordsMatched = queryWords.Count(queryWord =>
            descriptionWords.Any(descWord => descWord.Equals(queryWord, StringComparison.OrdinalIgnoreCase)));

        double titleMatchScore = (double)titleWordsMatched / queryWords.Length;
        double descriptionMatchScore = (double)descriptionWordsMatched / queryWords.Length;

        // Combine with configured weights
        return titleMatchScore * TitleWeight + descriptionMatchScore * DescriptionWeight;
    }

    /// <summary>
    /// Efficiently gets accessible project IDs for the current user without loading full DTOs.
    /// </summary>
    /// <param name="userSession">The current user session</param>
    /// <returns>Set of project IDs the user can access</returns>
    private async Task<HashSet<Guid>> GetAccessibleProjectIdsAsync(UserSession userSession)
    {
        if (userSession.User!.PermissionLevel == PermissionLevel.SuperAdmin)
            return await _context.Projects.Select(p => p.Id).ToHashSetAsync();

        if (userSession.User.PermissionLevel == PermissionLevel.SystemAdmin)

            return await _context.Projects
                .Where(p => (p.Lectorate != null && userSession.User.AssignedLectorates.Contains(p.Lectorate)) ||
                    (p.OwnerOrganisation != null &&
                        userSession.User.AssignedOrganisations.Contains(p.OwnerOrganisation)))
                .Select(p => p.Id)
                .ToHashSetAsync();

        List<string> accessibleSramIds = userSession.Collaborations
            .Select(c => c.CollaborationGroup.SCIMId)
            .ToList();

        return await _context.Projects
            .Where(p => accessibleSramIds.Contains(p.SCIMId))
            .Select(p => p.Id)
            .ToHashSetAsync();
    }

    /// <summary>
    /// Optimized hybrid score calculation.
    /// </summary>
    /// <param name="semanticSimilarity">The semantic similarity score</param>
    /// <param name="textMatchScore">The text match score</param>
    /// <returns>Combined hybrid score</returns>
    private static double CalculateHybridScoreOptimized(double semanticSimilarity, double textMatchScore) =>
        textMatchScore switch
        {
            >= 1.0 => PerfectMatchBoost + (semanticSimilarity * (1.0 - PerfectMatchBoost)),
            > 0    => (textMatchScore * TextMatchWeight) + (semanticSimilarity * SemanticWeight),
            _      => semanticSimilarity * NoMatchPenalty
        };
}
