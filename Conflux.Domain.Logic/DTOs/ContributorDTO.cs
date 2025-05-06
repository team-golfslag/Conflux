// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for <see cref="Contributor" />
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ContributorDTO
#pragma warning restore S101
{
    public required Person Person { get; init; }
    public required Guid ProjectId { get; init; }
    public List<ContributorRoleType> Roles { get; init; } = [];
    public List<ContributorPositionDTO> Positions { get; init; } = [];
    public bool Leader { get; init; }
    public bool Contact { get; init; }

    /// <summary>
    /// Converts a <see cref="ContributorDTO" /> to a <see cref="Contributor" />
    /// </summary>
    /// <returns>The converted <see cref="Contributor" /></returns>
    public Contributor ToContributor()
    {
        return new()
        {
            PersonId = Person.Id,
            ProjectId = ProjectId,
            Roles = Roles.ConvertAll(r => new ContributorRole
            {
                PersonId = Person.Id,
                ProjectId = ProjectId,
                RoleType = r,
            }),
            Positions = Positions.ConvertAll(p => new ContributorPosition
            {
                PersonId = Person.Id,
                ProjectId = ProjectId,
                Position = p.Type,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
            }),
            Leader = Leader,
            Contact = Contact,
        };
    }
}
