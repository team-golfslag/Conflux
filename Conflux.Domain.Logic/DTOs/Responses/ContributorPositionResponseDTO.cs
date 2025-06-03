// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Responses;

public class ContributorPositionResponseDTO
{
    public required Guid PersonId { get; init; }

    public required Guid ProjectId { get; init; }
    public required ContributorPositionType Position { get; init; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
