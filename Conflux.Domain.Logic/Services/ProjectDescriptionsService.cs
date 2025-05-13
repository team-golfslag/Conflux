// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Domain.Logic.Services;

public class ProjectDescriptionsService : IProjectDescriptionsService
{
    private readonly ConfluxContext _context;

    public ProjectDescriptionsService(ConfluxContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectDescriptionResponseDTO>> GetDescriptionsByProjectIdAsync(Guid projectId)
    {
        // Verify project exists
        bool projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            throw new ProjectNotFoundException(projectId);

        List<ProjectDescription> descriptions = await _context.ProjectDescriptions
            .Where(d => d.ProjectId == projectId)
            .ToListAsync();

        return descriptions.Select(MapToDescriptionResponseDTO).ToList();
    }

    public async Task<ProjectDescriptionResponseDTO> GetDescriptionByIdAsync(Guid projectId, Guid descriptionId)
    {
        ProjectDescription description = await GetDescriptionEntityAsync(projectId, descriptionId);
        return MapToDescriptionResponseDTO(description);
    }

    public async Task<ProjectDescriptionResponseDTO> CreateDescriptionAsync(Guid projectId,
        ProjectDescriptionRequestDTO descriptionDTO)
    {
        // Verify project exists
        bool projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            throw new ProjectNotFoundException(projectId);

        ProjectDescription description = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Text = descriptionDTO.Text,
            Type = descriptionDTO.Type,
            Language = descriptionDTO.Language,
        };

        _context.ProjectDescriptions.Add(description);
        await _context.SaveChangesAsync();

        return MapToDescriptionResponseDTO(description);
    }

    public async Task<ProjectDescriptionResponseDTO> UpdateDescriptionAsync(Guid projectId, Guid descriptionId,
        ProjectDescriptionRequestDTO descriptionDTO)
    {
        ProjectDescription description = await GetDescriptionEntityAsync(projectId, descriptionId);

        description.Text = descriptionDTO.Text;
        description.Type = descriptionDTO.Type;
        description.Language = descriptionDTO.Language;

        await _context.SaveChangesAsync();

        return MapToDescriptionResponseDTO(description);
    }

    public async Task DeleteDescriptionAsync(Guid projectId, Guid descriptionId)
    {
        ProjectDescription description = await GetDescriptionEntityAsync(projectId, descriptionId);

        _context.ProjectDescriptions.Remove(description);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Helper method to get a description entity
    /// </summary>
    private async Task<ProjectDescription> GetDescriptionEntityAsync(Guid projectId, Guid descriptionId)
    {
        ProjectDescription? description = await _context.ProjectDescriptions
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
            Language = description.Language,
        };
}
