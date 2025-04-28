// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs;

public class ProjectTitleDTO
{
    public required string Text { get; init; }
    public required TitleType Type { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }

    public ProjectTitle ToProjectTitle(Guid projectId) =>
        new()
        {
            ProjectId = projectId,
            Text = Text,
            Type = Type,
            StartDate = StartDate,
            EndDate = EndDate,
        };
}
