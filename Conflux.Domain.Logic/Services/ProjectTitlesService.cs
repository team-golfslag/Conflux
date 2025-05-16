// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.X509;

namespace Conflux.Domain.Logic.Services;

public class ProjectTitlesService : IProjectTitlesService
{
    private readonly ConfluxContext _context;

    public ProjectTitlesService(ConfluxContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectTitleResponseDTO>> GetTitlesByProjectIdAsync(Guid projectId)
    {
        await VerifyProjectExists(projectId);

        List<ProjectTitle> titles = await _context.ProjectTitles
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        return titles.Select(MapToTitleResponseDTO).ToList();
    }

    public async Task<ProjectTitle?> GetCurrentTitleByTitleType(Guid projectId, TitleType titleType)
    {
        await VerifyProjectExists(projectId);
        
        // ASSUMPTION: Here we assume there is only one current title of each type (so no multilingual titles!!)
        ProjectTitle? title = await _context.ProjectTitles
            .Where(t => t.ProjectId == projectId && t.Type == titleType && t.EndDate == null)
            .FirstOrDefaultAsync();

        return title;
    }

    public async Task<ProjectTitleResponseDTO> GetTitleByIdAsync(Guid projectId, Guid titleId)
    {
        return MapToTitleResponseDTO(await GetTitleEntityAsync(projectId, titleId));
    }

    public async Task<ProjectTitleResponseDTO> CreateTitleAsync(Guid projectId, ProjectTitleRequestDTO titleDTO)
    {
        await VerifyProjectExists(projectId);

        ProjectTitle? currentTitle = await GetCurrentTitleByTitleType(projectId, titleDTO.Type);

        DateTime today = DateTime.Today;
        
        if (currentTitle != null)
        {
            currentTitle.EndDate = today;
        }

        ProjectTitle newTitle = new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Text = titleDTO.Text,
            Language = titleDTO.Language,
            Type = titleDTO.Type,
            StartDate = today,
            EndDate = null,
        };

        _context.ProjectTitles.Add(newTitle);

        await _context.SaveChangesAsync();

        return MapToTitleResponseDTO(newTitle);
    }

    public async Task<ProjectTitleResponseDTO> EndTitleAsync(Guid projectId, Guid titleId)
    {
        await VerifyProjectExists(projectId);

        ProjectTitle title = await GetTitleEntityAsync(projectId, titleId);

        if (title.Type == TitleType.Primary)
        {
            throw new Exception("Can't end primary title.");
        }

        if (title.EndDate != null)
        {
            throw new Exception("End date was already set.");
        }
        
        title.EndDate = DateTime.Today;
        await _context.SaveChangesAsync();
        return MapToTitleResponseDTO(title);
    }
    
    public async Task<ProjectTitleResponseDTO> UpdateTitleAsync(Guid projectId, Guid titleId,
        ProjectTitleRequestDTO titleDTO)
    {
        await VerifyProjectExists(projectId);

        ProjectTitle title = await GetTitleEntityAsync(projectId, titleId);
        
        DateTime today = DateTime.Today;
        DateTime yesterday = today.Subtract(TimeSpan.FromDays(1));
        if (title.StartDate < yesterday)
        {
            throw new Exception("Can't edit a title that was made more than 1 day ago.");
        }

        if (title.EndDate != null)
        {
            throw new Exception("Can't edit a title that has already been succeeded.");
        }

        if (title.Type != titleDTO.Type)
        {
            throw new Exception("Can't edit a titles type. Try deleting the title instead.");
        }

        title.Language = titleDTO.Language;
        title.Text = titleDTO.Text;

        await _context.SaveChangesAsync();
        return MapToTitleResponseDTO(title);
    }
    

    public async Task DeleteDescriptionAsync(Guid projectId, Guid titleId) 
    {
        await VerifyProjectExists(projectId);

        ProjectTitle title = await GetTitleEntityAsync(projectId, titleId);
        
        DateTime today = DateTime.Today;
        DateTime yesterday = today.Subtract(TimeSpan.FromDays(1));
        if (title.StartDate < yesterday)
        {
            throw new Exception("Can't delete a title that was made more than 1 day ago.");
        }

        if (title.EndDate != null)
        {
            throw new Exception("Can't delete a title that has already been succeeded.");
        }

        ProjectTitle? oldTitle = await _context.ProjectTitles
            .Where(o => o.EndDate == title.StartDate && o.Type == title.Type)
            .FirstOrDefaultAsync();

        if (oldTitle != null)
        {
            oldTitle.EndDate = null;
        }
        else if (title.Type == TitleType.Primary)
        {
            throw new Exception("Can't delete primary title if there is no previous one.");
        }

        _context.ProjectTitles.Remove(title);
        await _context.SaveChangesAsync();
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
        ProjectTitle? title = await _context.ProjectTitles.FindAsync(projectId, titleId);
            // .SingleOrDefaultAsync(d => d.ProjectId == projectId && d.Id == titleId);

        if (title == null)
            throw new ProjectDescriptionNotFoundException(titleId);

        return title;
    }
    
    
    
    
    private ProjectTitleResponseDTO MapToTitleResponseDTO(ProjectTitle title) =>
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
