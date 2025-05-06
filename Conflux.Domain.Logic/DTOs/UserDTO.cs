// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs;

public class UserDTO
{
    public string? SRAMId { get; init; }
    public required string SCIMId { get; init; }
    public string? ORCiD { get; init; }
    public required string Name { get; init; }
    public List<UserRoleDTO> Roles { get; init; } = [];
    public string? GivenName { get; init; }
    public string? FamilyName { get; init; }
    public string? Email { get; init; }

    public User ToUser(Guid projectId)
    {
        Guid userId = Guid.NewGuid();
        return new()
        {
            Id = userId,
            SRAMId = SRAMId,
            SCIMId = SCIMId,
            ORCiD = ORCiD,
            Name = Name,
            Roles = Roles.Select(role => role.ToUserRole(userId, projectId)).ToList(),
            GivenName = GivenName,
            FamilyName = FamilyName,
            Email = Email,
        };
    }
}
