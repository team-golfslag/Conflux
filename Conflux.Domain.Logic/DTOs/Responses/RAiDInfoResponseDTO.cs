// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Responses;

public class RAiDInfoResponseDTO
{
    public Guid projectId { get; init; }

    public DateTime? LatestSync { get; set; }
    public bool Dirty { get; set; }

    public string RAiDId { get; init; }

    public required string RegistrationAgencyId { get; init; }

    public required string OwnerId { get; init; }
    public long? OwnerServicePoint { get; init; }

    public string License => "Creative Commons CC-0";
    public int Version { get; set; }
}
