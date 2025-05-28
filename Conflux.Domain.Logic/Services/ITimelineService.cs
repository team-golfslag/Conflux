// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Responses;

namespace Conflux.Domain.Logic.Services;

public interface ITimelineService
{
    public Task<List<TimelineItemResponseDTO>> GetTimelineItemsAsync(Guid projectId);
}
