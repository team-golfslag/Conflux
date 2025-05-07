// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs.Patch;

public class UserPatchDTO
{
    public string? SRAMId { get; init; }
    public string? SCIMId { get; init; }
    public string? ORCiD { get; init; }
    public string? Name { get; init; }
    public List<UserRoleDTO>? Roles { get; init; }
    public string? GivenName { get; init; }
    public string? FamilyName { get; init; }
    public string? Email { get; init; }
}
