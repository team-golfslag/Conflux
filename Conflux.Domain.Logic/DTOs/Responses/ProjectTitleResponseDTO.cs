// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Responses;

public class ProjectTitleResponseDTO
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }

    public required string Text { get; init; }

    public Language? Language { get; init; }

    public required TitleType Type { get; init; }

    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}
