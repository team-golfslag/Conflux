// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Conflux.Domain.Logic.Services;

public class ProjectTitlesService : IProjectTitlesService
{
    private readonly ConfluxContext _context;
    private readonly IProjectsService _projectsService;
    private readonly ILogger<ProjectTitlesService> _logger;

    public ProjectTitlesService(
        ConfluxContext context, 
        IProjectsService projectsService,
        ILogger<ProjectTitlesService> logger)
    {
        _context = context;
        _projectsService = projectsService;
        _logger = logger;
    }

    public async Task<List<ProjectTitleResponseDTO>> GetTitlesByProjectIdAsync(Guid projectId)
    {
        await VerifyProjectExists(projectId);

        List<ProjectTitle> titles = await _context.ProjectTitles
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        return titles.Select(MapToTitleResponseDTO).ToList();
    }

    public async Task<ProjectTitleResponseDTO?> GetCurrentTitleByTitleType(Guid projectId, TitleType titleType)
    {
        ProjectTitle? title = await GetCurrentTitleByTitleTypeHelper(projectId, titleType);
        return title == null ? null : MapToTitleResponseDTO(title);
    }

    public async Task<ProjectTitleResponseDTO> GetTitleByIdAsync(Guid projectId, Guid titleId) =>
        MapToTitleResponseDTO(await GetTitleEntityAsync(projectId, titleId));

    public async Task<List<ProjectTitleResponseDTO>> UpdateTitleAsync(Guid projectId, ProjectTitleRequestDTO titleDTO)
    {
        await VerifyProjectExists(projectId);

        ProjectTitle? currentTitle = await GetCurrentTitleByTitleTypeHelper(projectId, titleDTO.Type);

        DateTime today = DateTime.UtcNow.Date;

        if (currentTitle != null)
        {
            // If the previous title was created today we replace it instead of creating a new one.
            if (currentTitle.StartDate == today)
            {
                currentTitle.Language = titleDTO.Language;
                currentTitle.Text = titleDTO.Text;

                await _context.SaveChangesAsync();

                // Update project embedding asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _projectsService.UpdateProjectEmbeddingAsync(projectId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update embedding for project {ProjectId} after title update", projectId);
                    }
                });

                return await GetTitlesByProjectIdAsync(projectId);
            }

            currentTitle.EndDate = today;
        }

        ProjectTitle newTitle = new()
        {
            Id = Guid.CreateVersion7(),
            ProjectId = projectId,
            Text = titleDTO.Text,
            Language = titleDTO.Language,
            Type = titleDTO.Type,
            StartDate = today,
            EndDate = null,
        };

        _context.ProjectTitles.Add(newTitle);

        await _context.SaveChangesAsync();

        // Update project embedding asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _projectsService.UpdateProjectEmbeddingAsync(projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update embedding for project {ProjectId} after title creation", projectId);
            }
        });

        return await GetTitlesByProjectIdAsync(projectId);
    }

    public async Task<ProjectTitleResponseDTO> EndTitleAsync(Guid projectId, Guid titleId)
    {
        ProjectTitle title = await GetTitleEntityAsync(projectId, titleId);

        if (title.Type == TitleType.Primary) throw new CantEndPrimaryTitleException(titleId);

        if (title.EndDate != null) throw new CantEndEndedTitleException(titleId);

        title.EndDate = DateTime.UtcNow.Date;
        await _context.SaveChangesAsync();

        // Update project embedding asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _projectsService.UpdateProjectEmbeddingAsync(projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update embedding for project {ProjectId} after title end", projectId);
            }
        });

        return MapToTitleResponseDTO(title);
    }


    public async Task DeleteTitleAsync(Guid projectId, Guid titleId)
    {
        ProjectTitle title = await GetTitleEntityAsync(projectId, titleId);

        DateTime today = DateTime.UtcNow.Date;
        DateTime yesterday = today.Subtract(TimeSpan.FromDays(1));
        if (title.StartDate < yesterday || title.EndDate != null) throw new CantDeleteTitleException(projectId);

        ProjectTitle? oldTitle = await _context.ProjectTitles
            .Where(o => o.EndDate == title.StartDate && o.Type == title.Type)
            .OrderByDescending(o => o.StartDate)
            .FirstOrDefaultAsync();

        if (oldTitle != null)
            oldTitle.EndDate = null;
        else if (title.Type == TitleType.Primary) throw new CantDeleteTitleException(titleId);

        _context.ProjectTitles.Remove(title);
        await _context.SaveChangesAsync();

        // Update project embedding asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _projectsService.UpdateProjectEmbeddingAsync(projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update embedding for project {ProjectId} after title deletion", projectId);
            }
        });
    }

    internal async Task<ProjectTitle?> GetCurrentTitleByTitleTypeHelper(Guid projectId, TitleType titleType)
    {
        await VerifyProjectExists(projectId);

        // ASSUMPTION: Here we assume there is only one current title of each type (so no multilingual titles!!)
        ProjectTitle? title = await _context.ProjectTitles
            .Where(t => t.ProjectId == projectId && t.Type == titleType && t.EndDate == null)
            .FirstOrDefaultAsync();

        return title;
    }

    private async Task VerifyProjectExists(Guid projectId)
    {
        // Verify project exists
        bool projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            throw new ProjectNotFoundException(projectId);
    }

    /// <summary>
    /// Helper method to get a description entity
    /// </summary>
    private async Task<ProjectTitle> GetTitleEntityAsync(Guid projectId, Guid titleId)
    {
        await VerifyProjectExists(projectId);

        ProjectTitle? title = await _context.ProjectTitles
            .SingleOrDefaultAsync(d => d.ProjectId == projectId && d.Id == titleId);

        if (title == null)
            throw new ProjectTitleNotFoundException(titleId);

        return title;
    }


    private static ProjectTitleResponseDTO MapToTitleResponseDTO(ProjectTitle title) =>
        new()
        {
            Id = title.Id,
            ProjectId = title.ProjectId,
            Text = title.Text,
            Language = title.Language,
            Type = title.Type,
            StartDate = title.StartDate,
            EndDate = title.EndDate,
        };
}
