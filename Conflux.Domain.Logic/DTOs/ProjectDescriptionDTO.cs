// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs;

public class ProjectDescriptionDTO
{
    public required string Text { get; set; }

    public DescriptionType Type { get; init; }

    public Language? Language { get; set; }

    public ProjectDescription ToProjectDescription(Guid projectId) =>
        new()
        {
            ProjectId = projectId,
            Text = Text,
            Language = Language,
            Type = Type,
        };
}
