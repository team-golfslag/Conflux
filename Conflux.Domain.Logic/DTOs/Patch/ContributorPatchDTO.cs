// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Patch;

/// <summary>
/// The Data Transfer Object for patching a <see cref="Contributor" /> with PATCH.
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ContributorPatchDTO
#pragma warning restore S101
{
    public List<ContributorRoleType>? Roles { get; init; }
    public List<ContributorPositionRequestDTO>? Positions { get; init; }
    public bool? Leader { get; init; }
    public bool? Contact { get; init; }
}
