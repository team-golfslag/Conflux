// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

public class TimelineService(IProjectsService projectsService) : ITimelineService
{
    private readonly IProjectsService _projectsService = projectsService;

    public async Task<List<TimelineItemResponseDTO>> GetTimelineItemsAsync(Guid projectId)
    {
        Project project = await _projectsService.GetProjectByIdAsync(projectId);
        List<TimelineItemResponseDTO> timelineItems =
        [
            new()
            {
                IsMilestone = true,
                Date = project.StartDate,
                ShortDescription = "Project started",
                Description = $"The project started on {project.StartDate:dd MMMM yyyy}.",
            },
        ];
        
        if (project.EndDate.HasValue)
            timelineItems.Add(new()
            {
                IsMilestone = true,
                Date = project.EndDate.Value,
                ShortDescription = "Project ended",
                Description = $"The project ended on {project.EndDate.Value:dd MMMM yyyy}.",
            });

        timelineItems.AddRange(GetTimelineItemsForTitles(project));
        timelineItems.AddRange(GetTimelineItemsForOrganisations(project));
        timelineItems.AddRange(GetTimelineItemsForContributors(project));
        
        return timelineItems.OrderByDescending(item => item.Date).ToList();
    }

    private List<TimelineItemResponseDTO> GetTimelineItemsForTitles(Project project)
    {
        List<TimelineItemResponseDTO> timelineItems = [];
        timelineItems.AddRange(project.Titles.Select(title => new TimelineItemResponseDTO
        {
            IsMilestone = false,
            Date = title.StartDate,
            ShortDescription = "Title updated",
            Description = $"The project {title.Type}-{title.Language?.Id} title was updated to '{title.Text}' on {title.StartDate:dd MMMM yyyy}.",
        }));

        return timelineItems;
    }

    private List<TimelineItemResponseDTO> GetTimelineItemsForOrganisations(Project project)
    {
        List<TimelineItemResponseDTO> timelineItems = [];
        foreach (ProjectOrganisation organisation in project.Organisations)
        {
            if (organisation.Roles.Count == 0)
                continue;
            DateTime startDate = organisation.Roles.Min(r => r.StartDate);

            timelineItems.Add(new()
            {
                IsMilestone = false,
                Date = startDate,
                ShortDescription = "Organisation added",
                Description = organisation.Organisation != null ? $"The organisation '{organisation.Organisation.Name}' was added to the project on {startDate:dd MMMM yyyy}." : "",
            });
            
            DateTime? endDate = organisation.Roles.All(r => r.EndDate.HasValue) ? organisation.Roles.Max(r => r.EndDate.Value) : (DateTime?)null;
            if (endDate.HasValue)
                timelineItems.Add(new()
                {
                    IsMilestone = false,
                    Date = endDate.Value,
                    ShortDescription = "Organisation left",
                    Description = organisation.Organisation != null ? $"The organisation '{organisation.Organisation.Name}' left the project on {endDate.Value:dd MMMM yyyy}." : "",
                });
        }
        
        return timelineItems;
    }

    private List<TimelineItemResponseDTO> GetTimelineItemsForContributors(Project project)
    {
        List<TimelineItemResponseDTO> timelineItems = [];
        foreach (Contributor contributor in project.Contributors)
        {
            if (contributor.Positions.Count == 0)
                continue;
            DateTime startDate = contributor.Positions.Min(p => p.StartDate);
            timelineItems.Add(new()
            {
                IsMilestone = false,
                Date = startDate,
                ShortDescription = "Contributor added",
                Description = contributor.Person != null ? $"The contributor '{contributor.Person.Name}' was added to the project on {startDate:dd MMMM yyyy}." : "",
            });
            
            DateTime? endDate = contributor.Positions.All(p => p.EndDate.HasValue) ? contributor.Positions.Max(p => p.EndDate.Value) : (DateTime?)null;
            if (endDate.HasValue)
                timelineItems.Add(new()
                {
                    IsMilestone = false,
                    Date = endDate.Value,
                    ShortDescription = "Contributor left",
                    Description = contributor.Person != null ? $"The contributor '{contributor.Person.Name}' left the project on {endDate.Value:dd MMMM yyyy}." : "",
                });
        }
        
        return timelineItems;
    }
}
