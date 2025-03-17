using Conflux.Domain;

namespace Conflux.RepositoryConnections.NWOpen;

/// <summary>
/// Represents seed data.
/// </summary>
public class SeedData
{
    public List<Party> Parties { get; } = [];
    public List<Person> People { get; }= [];
    public List<Product> Products { get;  }= [];
    public List<Project> Projects { get;  }= [];
}
