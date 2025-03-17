using Conflux.Domain;

namespace Conflux.RepositoryConnections.NWOpen;

/// <summary>
/// Represents seed data.
/// </summary>
public class SeedData
{
    public List<Party> Parties { get; init; } = [];
    public List<Person> People { get; init; } = [];
    public List<Product> Products { get; init; } = [];
    public List<Project> Projects { get; init; } = [];
}
