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

/// <summary>
/// Service for managing project descriptions.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="projectsService">The projects service for embedding updates.</param>
/// <param name="logger">The logger.</param>
public class ProjectDescriptionsService(
    ConfluxContext context, 
    IProjectsService projectsService,
    ILogger<ProjectDescriptionsService> logger) : IProjectDescriptionsService
{
    /// <inheritdoc />
    public async Task<List<ProjectDescriptionResponseDTO>> GetDescriptionsByProjectIdAsync(Guid projectId)
    {
        // Verify project exists
        bool projectExists = await context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            throw new ProjectNotFoundException(projectId);

        List<ProjectDescription> descriptions = await context.ProjectDescriptions
            .Where(d => d.ProjectId == projectId)
            .ToListAsync();

        return descriptions.Select(MapToDescriptionResponseDTO).ToList();
    }

    /// <inheritdoc />
    public async Task<ProjectDescriptionResponseDTO> GetDescriptionByIdAsync(Guid projectId, Guid descriptionId)
    {
        ProjectDescription description = await GetDescriptionEntityAsync(projectId, descriptionId);
        return MapToDescriptionResponseDTO(description);
    }

    /// <inheritdoc />
    public async Task<ProjectDescriptionResponseDTO> CreateDescriptionAsync(Guid projectId,
        ProjectDescriptionRequestDTO descriptionDTO)
    {
        // Verify project exists
        bool projectExists = await context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            throw new ProjectNotFoundException(projectId);

        ProjectDescription description = new()
        {
            Id = Guid.CreateVersion7(),
            ProjectId = projectId,
            Text = descriptionDTO.Text,
            Type = descriptionDTO.Type,
            Language = descriptionDTO.Language,
        };

        context.ProjectDescriptions.Add(description);
        await context.SaveChangesAsync();

        // Update project embedding asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await projectsService.UpdateProjectEmbeddingAsync(projectId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update embedding for project {ProjectId} after description creation", projectId);
            }
        });

        return MapToDescriptionResponseDTO(description);
    }

    /// <inheritdoc />
    public async Task<ProjectDescriptionResponseDTO> UpdateDescriptionAsync(Guid projectId, Guid descriptionId,
        ProjectDescriptionRequestDTO descriptionDTO)
    {
        ProjectDescription description = await GetDescriptionEntityAsync(projectId, descriptionId);

        description.Text = descriptionDTO.Text;
        description.Type = descriptionDTO.Type;
        description.Language = descriptionDTO.Language;

        await context.SaveChangesAsync();

        // Update project embedding asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await projectsService.UpdateProjectEmbeddingAsync(projectId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update embedding for project {ProjectId} after description update", projectId);
            }
        });

        return MapToDescriptionResponseDTO(description);
    }

    /// <inheritdoc />
    public async Task DeleteDescriptionAsync(Guid projectId, Guid descriptionId)
    {
        ProjectDescription description = await GetDescriptionEntityAsync(projectId, descriptionId);

        context.ProjectDescriptions.Remove(description);
        await context.SaveChangesAsync();

        // Update project embedding asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await projectsService.UpdateProjectEmbeddingAsync(projectId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update embedding for project {ProjectId} after description deletion", projectId);
            }
        });
    }

    /// <summary>
    /// Helper method to get a description entity
    /// </summary>
    private async Task<ProjectDescription> GetDescriptionEntityAsync(Guid projectId, Guid descriptionId)
    {
        ProjectDescription? description = await context.ProjectDescriptions
            .SingleOrDefaultAsync(d => d.ProjectId == projectId && d.Id == descriptionId);

        if (description == null)
            throw new ProjectDescriptionNotFoundException(descriptionId);

        return description;
    }

    /// <summary>
    /// Maps a description entity to a description response DTO
    /// </summary>
    private static ProjectDescriptionResponseDTO MapToDescriptionResponseDTO(ProjectDescription description) =>
        new()
        {
            Id = description.Id,
            ProjectId = description.ProjectId,
            Text = description.Text,
            Type = description.Type,
            Language = description.Language ?? Language.DUTCH,
        };
}
