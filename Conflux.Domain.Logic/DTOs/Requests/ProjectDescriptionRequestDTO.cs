// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Requests;

public class ProjectDescriptionRequestDTO
{
    public required string Text { get; set; }

    public required DescriptionType Type { get; init; }

    public Language? Language { get; set; }
}
