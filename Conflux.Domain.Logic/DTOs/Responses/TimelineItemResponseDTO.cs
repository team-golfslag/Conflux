// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Responses;

public class TimelineItemResponseDTO
{
    public bool IsMilestone { get; init; }
    public DateTime Date { get; init; }
    public string ShortDescription { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
