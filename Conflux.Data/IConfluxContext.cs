// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Data;

/// <summary>
/// Abstraction of the Conflux EF Core DbContext for easier testing.
/// </summary>
public interface IConfluxContext
{
    DbSet<Person> People { get; }
    DbSet<User> Users { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Contributor> Contributors { get; }
    DbSet<ContributorRole> ContributorRoles { get; }
    DbSet<ContributorPosition> ContributorPositions { get; }
    DbSet<Product> Products { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectTitle> ProjectTitles { get; }
    DbSet<Organisation> Organisations { get; }
    DbSet<OrganisationRole> OrganisationRoles { get; }
    DbSet<SRAMGroupIdConnection> SRAMGroupIdConnections { get; }

    /// <summary>
    /// Persists all changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the save operation.</param>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
