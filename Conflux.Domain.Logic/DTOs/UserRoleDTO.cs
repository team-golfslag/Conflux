// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs;

public class UserRoleDTO
{
    public required UserRoleType Type { get; init; }
    public required string Urn { get; init; }
    public required string SCIMId { get; init; }

    public UserRole ToUserRole(Guid userId, Guid projectId) =>
        new()
        {
            Id = userId,
            ProjectId = projectId,
            Type = Type,
            Urn = Urn,
            SCIMId = SCIMId,
        };
}
